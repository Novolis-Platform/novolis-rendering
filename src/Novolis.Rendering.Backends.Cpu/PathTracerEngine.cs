using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Cpu;

internal static class PathTracerEngine
{
    private const int MaxDepth = 4;
    private const float Epsilon = 1e-4f;
    private const int TileSize = 32;

    public static void RenderSample(
        Vector3[] accumulation,
        Rgba32[] display,
        int width,
        int height,
        CameraSnapshot camera,
        CompiledScene scene,
        int sampleIndex,
        bool deterministic)
    {
        var invCount = 1f / (sampleIndex + 1);
        var tanHalfFov = MathF.Tan(camera.VerticalFovRadians * 0.5f);
        var aspect = (float)width / height;
        var tilesX = (width + TileSize - 1) / TileSize;
        var tilesY = (height + TileSize - 1) / TileSize;
        var tileCount = tilesX * tilesY;
        var options = deterministic
            ? new ParallelOptions { MaxDegreeOfParallelism = 1 }
            : new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        Parallel.For(0, tileCount, options, tileIndex =>
        {
            var tileY = tileIndex / tilesX;
            var tileX = tileIndex % tilesX;
            var y0 = tileY * TileSize;
            var y1 = System.Math.Min(y0 + TileSize, height);
            var x0 = tileX * TileSize;
            var x1 = System.Math.Min(x0 + TileSize, width);

            var rng = deterministic
                ? new Random(42 + sampleIndex * 100_000 + tileIndex * 7919)
                : new Random(HashCode.Combine(tileIndex, sampleIndex, Environment.TickCount));

            for (var y = y0; y < y1; y++)
            {
                for (var x = x0; x < x1; x++)
                {
                    var jitterX = deterministic ? 0.5f : (float)rng.NextDouble();
                    var jitterY = deterministic ? 0.5f : (float)rng.NextDouble();
                    var u = (2f * (x + jitterX) / width - 1f) * tanHalfFov * aspect;
                    var v = (2f * (y + jitterY) / height - 1f) * tanHalfFov;
                    var dir = Vector3.Normalize(camera.Forward + u * camera.Right + v * camera.Up);
                    var ray = new Ray(camera.Position, dir);
                    var radiance = Trace(in ray, scene, 0, ref rng);
                    var idx = y * width + x;
                    var blended = accumulation[idx] + radiance;
                    accumulation[idx] = blended;
                    display[idx] = ToRgba(blended * invCount);
                }
            }
        });
    }

    private static Vector3 Trace(in Ray ray, CompiledScene scene, int depth, ref Random rng)
    {
        if (!IntersectScene(in ray, scene, out var hit))
        {
            return SampleSky(ray.Direction);
        }

        var mat = scene.Materials[hit.MaterialIndex];
        return mat.Model switch
        {
            MaterialModel.Emissive => EmissiveShade(mat),
            MaterialModel.Glass => GlassShade(in ray, in hit, scene, depth, ref rng, mat),
            MaterialModel.Skin => SkinShade(in ray, in hit, scene, depth, ref rng, mat),
            _ => StandardShade(in ray, in hit, scene, depth, ref rng, mat),
        };
    }

    private static Vector3 StandardShade(
        in Ray ray,
        in Hit hit,
        CompiledScene scene,
        int depth,
        ref Random rng,
        GpuMaterial mat)
    {
        var baseColor = new Vector3(mat.A.X, mat.A.Y, mat.A.Z);
        var roughness = MathF.Max(mat.A.W, 0.04f);
        var metallic = mat.B.X;
        var emissionStrength = mat.B.Y;
        var emissionColor = new Vector3(mat.C.X, mat.C.Y, mat.C.Z);

        var n = hit.Normal;
        var v = Vector3.Normalize(-ray.Direction);
        var radiance = emissionColor * emissionStrength;

        foreach (var light in scene.Lights)
        {
            var lightDir = light.Kind == GpuLightKind.Directional
                ? Vector3.Normalize(-light.DirectionOrPosition)
                : Vector3.Normalize(light.DirectionOrPosition - hit.Point);

            if (!ShadowRayClear(hit.Point, lightDir, scene, light.Kind == GpuLightKind.Directional ? 1e6f : (light.DirectionOrPosition - hit.Point).Length()))
            {
                continue;
            }

            var ndotl = MathF.Max(0f, Vector3.Dot(n, lightDir));
            var diffuse = baseColor * (1f - metallic);
            var spec = GgxSpec(n, v, lightDir, roughness, Vector3.One);
            radiance += (diffuse * ndotl + spec) * light.Color * light.Intensity;
        }

        radiance += baseColor * 0.03f;

        if (depth < MaxDepth && metallic > 0.75f)
        {
            var mirrorWeight = metallic * (1f - MathF.Min(roughness / 0.12f, 1f));
            if (mirrorWeight > 0.01f)
            {
                var reflectDir = Vector3.Reflect(ray.Direction, n);
                var reflectRay = new Ray(hit.Point + n * Epsilon, Vector3.Normalize(reflectDir));
                radiance += baseColor * Trace(in reflectRay, scene, depth + 1, ref rng) * mirrorWeight;
            }
        }

        if (depth < MaxDepth && roughness > 0.01f && rng.NextDouble() > metallic)
        {
            var bounceDir = CosineHemisphere(n, ref rng);
            var bounce = new Ray(hit.Point + n * Epsilon, bounceDir);
            radiance += baseColor * Trace(in bounce, scene, depth + 1, ref rng) * 0.5f;
        }

        return radiance;
    }

    private static Vector3 GlassShade(
        in Ray ray,
        in Hit hit,
        CompiledScene scene,
        int depth,
        ref Random rng,
        GpuMaterial mat)
    {
        if (depth >= MaxDepth)
        {
            return new Vector3(mat.A.X, mat.A.Y, mat.A.Z);
        }

        var tint = new Vector3(mat.A.X, mat.A.Y, mat.A.Z);
        var ior = MathF.Max(mat.B.X, 1f);
        var n = hit.Normal;
        var cosTheta = MathF.Min(1f, MathF.Max(-1f, Vector3.Dot(-ray.Direction, n)));
        var entering = cosTheta > 0f;
        if (!entering)
        {
            n = -n;
            cosTheta = -cosTheta;
        }

        var eta = entering ? 1f / ior : ior;
        var k = 1f - eta * eta * (1f - cosTheta * cosTheta);
        Vector3 refractDir;
        if (k < 0f)
        {
            refractDir = Vector3.Reflect(ray.Direction, n);
        }
        else
        {
            refractDir = Vector3.Normalize(eta * ray.Direction + (eta * cosTheta - MathF.Sqrt(k)) * n);
        }

        var refractRay = new Ray(hit.Point + refractDir * Epsilon, refractDir);
        return tint * Trace(in refractRay, scene, depth + 1, ref rng);
    }

    private static Vector3 SkinShade(
        in Ray ray,
        in Hit hit,
        CompiledScene scene,
        int depth,
        ref Random rng,
        GpuMaterial mat)
    {
        var baseColor = new Vector3(mat.A.X, mat.A.Y, mat.A.Z);
        var subsurface = mat.B.X;
        var blood = new Vector3(mat.C.X, mat.C.Y, mat.C.Z);
        var standardMat = new GpuMaterial(MaterialModel.Standard, mat.A, mat.B, mat.C);
        var standard = StandardShade(in ray, in hit, scene, depth, ref rng, standardMat);
        return Vector3.Lerp(standard, blood * subsurface + baseColor * 0.5f, subsurface * 0.35f);
    }

    private static Vector3 EmissiveShade(GpuMaterial mat)
    {
        var color = new Vector3(mat.A.X, mat.A.Y, mat.A.Z);
        return color * mat.A.W;
    }

    private static Vector3 GgxSpec(Vector3 n, Vector3 v, Vector3 l, float roughness, Vector3 f0)
    {
        var h = Vector3.Normalize(v + l);
        var ndoth = MathF.Max(0f, Vector3.Dot(n, h));
        var ndotl = MathF.Max(0f, Vector3.Dot(n, l));
        var ndotv = MathF.Max(0f, Vector3.Dot(n, v));
        if (ndotl <= 0f || ndotv <= 0f)
        {
            return Vector3.Zero;
        }

        var a = roughness * roughness;
        var a2 = a * a;
        var denom = ndoth * ndoth * (a2 - 1f) + 1f;
        var d = a2 / (MathF.PI * denom * denom + 1e-7f);
        return f0 * d * ndotl;
    }

    private static Vector3 CosineHemisphere(Vector3 normal, ref Random rng)
    {
        var u1 = (float)rng.NextDouble();
        var u2 = (float)rng.NextDouble();
        var r = MathF.Sqrt(u1);
        var phi = 2f * MathF.PI * u2;
        var x = r * MathF.Cos(phi);
        var y = r * MathF.Sin(phi);
        var z = MathF.Sqrt(MathF.Max(0f, 1f - u1));
        var up = MathF.Abs(normal.Y) < 0.999f ? Vector3.UnitY : Vector3.UnitX;
        var tangent = Vector3.Normalize(Vector3.Cross(up, normal));
        var bitangent = Vector3.Cross(normal, tangent);
        return Vector3.Normalize(tangent * x + bitangent * y + normal * z);
    }

    private static bool ShadowRayClear(Vector3 origin, Vector3 dir, CompiledScene scene, float maxDist)
    {
        var ray = new Ray(origin + dir * Epsilon, dir);
        return !IntersectScene(in ray, scene, out _, maxDist);
    }

    private static bool IntersectScene(in Ray ray, CompiledScene scene, out Hit hit, float maxDistance = float.MaxValue)
    {
        hit = default;
        if (scene.BvhRootIndex < 0 || scene.BvhNodes.Length == 0)
        {
            return IntersectBruteForce(in ray, scene, out hit, maxDistance);
        }

        bool HitTriangle(int triIdx, in Ray r, float maxT, out float t, out Vector3 n) =>
            TryHitGpuTriangle(scene, triIdx, in r, maxT, out t, out n);

        if (!BvhRaycast.Traverse(
                scene.BvhNodes.AsSpan(),
                scene.TriangleOrder.AsSpan(),
                scene.BvhRootIndex,
                in ray,
                maxDistance,
                HitTriangle,
                out var distance,
                out var normal,
                out var triangleIndex))
        {
            return false;
        }

        var materialIndex = scene.Triangles[triangleIndex].MaterialIndex;
        hit = new Hit(materialIndex, ray.PointAt(distance), normal);
        return true;
    }

    private static bool TryHitGpuTriangle(CompiledScene scene, int triIdx, in Ray ray, float maxT, out float t, out Vector3 normal)
    {
        var tri = scene.Triangles[triIdx];
        var v0 = new Vector3(tri.A.X, tri.A.Y, tri.A.Z);
        var v1 = new Vector3(tri.B.X, tri.B.Y, tri.B.Z);
        var v2 = new Vector3(tri.C.X, tri.C.Y, tri.C.Z);
        return TriangleRay.TryHit(in ray, v0, v1, v2, maxT, out t, out normal);
    }

    private static bool IntersectBruteForce(in Ray ray, CompiledScene scene, out Hit hit, float maxDistance)
    {
        hit = default;
        var bestT = maxDistance;
        var found = false;
        for (var i = 0; i < scene.Triangles.Length; i++)
        {
            var tri = scene.Triangles[i];
            var v0 = new Vector3(tri.A.X, tri.A.Y, tri.A.Z);
            var v1 = new Vector3(tri.B.X, tri.B.Y, tri.B.Z);
            var v2 = new Vector3(tri.C.X, tri.C.Y, tri.C.Z);
            if (!TriangleRay.TryHit(in ray, v0, v1, v2, bestT, out var t, out var n))
            {
                continue;
            }

            found = true;
            bestT = t;
            hit = new Hit(tri.MaterialIndex, ray.PointAt(t), n);
        }

        return found;
    }

    private static Vector3 SampleSky(Vector3 direction)
    {
        var t = System.Math.Clamp(direction.Y * 0.5f + 0.5f, 0f, 1f);
        var low = new Vector3(40f / 255f, 44f / 255f, 52f / 255f);
        var high = new Vector3(120f / 255f, 168f / 255f, 220f / 255f);
        return Vector3.Lerp(low, high, t);
    }

    private static Rgba32 ToRgba(Vector3 c)
    {
        static byte ToByte(float v) => (byte)System.Math.Clamp((int)(v * 255f), 0, 255);
        return new Rgba32(ToByte(c.X), ToByte(c.Y), ToByte(c.Z));
    }

    private readonly record struct Hit(int MaterialIndex, Vector3 Point, Vector3 Normal);
}
