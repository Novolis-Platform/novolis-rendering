using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Cpu;

/// <summary>
/// CPU path tracer. Bounce math aligned with the ILGPU kernel (Lague-style throughput,
/// Russian roulette, Fresnel glass, sky + sun disc).
/// </summary>
internal static class PathTracerEngine
{
    private const int AbsoluteMaxDepth = 6;
    private const float Epsilon = 1e-4f;
    private const int TileSize = 32;
    private const float DisplayExposure = 1.25f;
    private const float EnvStrength = 0.04f;
    private const float FireflyClamp = 6f;

    public static void RenderSample(
        Vector3[] accumulation,
        Rgba32[] display,
        int width,
        int height,
        CameraSnapshot camera,
        CompiledScene scene,
        int sampleIndex,
        bool deterministic,
        int maxDepth = AbsoluteMaxDepth)
    {
        if (maxDepth < 1)
        {
            maxDepth = 1;
        }
        else if (maxDepth > AbsoluteMaxDepth)
        {
            maxDepth = AbsoluteMaxDepth;
        }

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
                    // Row 0 is the top of the framebuffer (matches Silk/OpenGL blit UVs).
                    var v = (1f - 2f * (y + jitterY) / height) * tanHalfFov;
                    var dir = Vector3.Normalize(camera.Forward + u * camera.Right + v * camera.Up);
                    var radiance = ClampRadiance(TracePath(camera.Position, dir, scene, ref rng, maxDepth));
                    var idx = y * width + x;
                    // sampleIndex 0 replaces so interactive camera moves skip a full accumulation clear.
                    var blended = sampleIndex == 0 ? radiance : accumulation[idx] + radiance;
                    accumulation[idx] = blended;
                    display[idx] = ToRgba(blended * invCount);
                }
            }
        });
    }

    private static Vector3 TracePath(Vector3 origin, Vector3 direction, CompiledScene scene, ref Random rng, int maxDepth)
    {
        var throughput = Vector3.One;
        var radiance = Vector3.Zero;

        for (var depth = 0; depth < maxDepth; depth++)
        {
            var ray = new Ray(origin, direction);
            if (!IntersectScene(in ray, scene, out var hit))
            {
                radiance += throughput * SampleSky(direction, scene);
                break;
            }

            var mat = scene.Materials[hit.MaterialIndex];
            if (mat.Model == MaterialModel.Emissive)
            {
                if (depth == 0)
                {
                    radiance += throughput * new Vector3(mat.A.X, mat.A.Y, mat.A.Z) * mat.A.W;
                }

                break;
            }

            if (mat.Model == MaterialModel.Glass)
            {
                if (!ScatterGlass(ref origin, ref direction, ref throughput, in hit, mat, ref rng))
                {
                    break;
                }
            }
            else
            {
                if (depth < 3)
                {
                    radiance += throughput * DirectLighting(in hit, scene, mat, ref rng);
                }

                if (mat.Model == MaterialModel.Standard && mat.B.Y > 1e-5f)
                {
                    radiance += throughput * new Vector3(mat.C.X, mat.C.Y, mat.C.Z) * mat.B.Y;
                }

                if (!ScatterStandard(ref origin, ref direction, ref throughput, in hit, mat, ref rng))
                {
                    break;
                }
            }

            if (depth >= 2)
            {
                var p = MathF.Min(MathF.Max(throughput.X, MathF.Max(throughput.Y, throughput.Z)), 0.9f);
                if (rng.NextDouble() >= p)
                {
                    break;
                }

                throughput /= MathF.Max(p, 1e-4f);
            }
        }

        return radiance;
    }

    private static bool ScatterStandard(
        ref Vector3 origin,
        ref Vector3 direction,
        ref Vector3 throughput,
        in Hit hit,
        GpuMaterial mat,
        ref Random rng)
    {
        var baseColor = ResolveBaseColor(mat, hit.Point);
        var roughness = MathF.Max(mat.A.W, 0.04f);
        var metallic = mat.Model is MaterialModel.Skin or MaterialModel.Checker
            ? 0f
            : System.Math.Clamp(mat.B.X, 0f, 1f);
        if (mat.Model == MaterialModel.Skin)
        {
            var subsurface = System.Math.Clamp(mat.B.X, 0f, 1f);
            var blood = new Vector3(mat.C.X, mat.C.Y, mat.C.Z);
            baseColor = Vector3.Lerp(baseColor, blood, subsurface * 0.35f);
        }

        var smoothness = 1f - roughness;
        var specularProbability = System.Math.Clamp(metallic * 0.85f + 0.05f, 0.05f, 1f);
        var n = hit.Normal;

        var isSpecular = rng.NextDouble() < specularProbability;
        var diffuseDir = Vector3.Normalize(n + RandomDirection(ref rng));
        if (Vector3.Dot(diffuseDir, n) <= 0f)
        {
            diffuseDir = Vector3.Normalize(n + RandomDirection(ref rng) * 0.5f);
        }

        var specularDir = Vector3.Normalize(Vector3.Reflect(direction, n));
        direction = isSpecular
            ? Vector3.Normalize(Vector3.Lerp(diffuseDir, specularDir, smoothness))
            : diffuseDir;
        origin = hit.Point + n * Epsilon;

        var surface = isSpecular
            ? Vector3.Lerp(new Vector3(0.04f), baseColor, metallic)
            : baseColor * (1f - metallic * 0.85f);
        throughput *= surface;
        return throughput.X + throughput.Y + throughput.Z > 1e-5f;
    }

    private static bool ScatterGlass(
        ref Vector3 origin,
        ref Vector3 direction,
        ref Vector3 throughput,
        in Hit hit,
        GpuMaterial mat,
        ref Random rng)
    {
        var tint = new Vector3(mat.A.X, mat.A.Y, mat.A.Z);
        var roughness = System.Math.Clamp(mat.A.W, 0f, 1f);
        var ior = MathF.Max(mat.B.X, 1.01f);
        var absorption = MathF.Max(mat.B.Y, 0f);
        var n = hit.Normal;
        var entering = Vector3.Dot(direction, n) < 0f;
        if (!entering)
        {
            n = -n;
            if (absorption > 1e-5f)
            {
                var atten = MathF.Exp(-hit.Distance * absorption);
                throughput *= tint * atten;
            }
        }

        var iorA = entering ? 1f : ior;
        var iorB = entering ? ior : 1f;
        var reflectWeight = FresnelReflectance(direction, n, iorA, iorB);
        var reflectDir = Vector3.Normalize(Vector3.Reflect(direction, n));
        var refractDir = Refract(direction, n, iorA, iorB);
        var hasRefract = refractDir.LengthSquared() > 1e-8f;

        var diffuseDir = Vector3.Normalize(n + RandomDirection(ref rng));
        reflectDir = Vector3.Normalize(Vector3.Lerp(diffuseDir, reflectDir, 1f - roughness * 0.85f));
        if (hasRefract)
        {
            refractDir = Vector3.Normalize(Vector3.Lerp(-diffuseDir, refractDir, 1f - roughness));
        }

        var followReflection = !hasRefract || rng.NextDouble() <= reflectWeight;
        direction = followReflection ? reflectDir : refractDir;
        origin = hit.Point + n * Epsilon * (followReflection ? 1f : -1f);
        if (!followReflection)
        {
            throughput *= tint;
        }

        return throughput.X + throughput.Y + throughput.Z > 1e-5f;
    }

    private static float FresnelReflectance(Vector3 inDir, Vector3 normal, float iorA, float iorB)
    {
        var ratio = iorA / iorB;
        var cosIn = System.Math.Clamp(-Vector3.Dot(inDir, normal), 0f, 1f);
        var sin2 = ratio * ratio * (1f - cosIn * cosIn);
        if (sin2 >= 1f)
        {
            return 1f;
        }

        var cosT = MathF.Sqrt(MathF.Max(0f, 1f - sin2));
        var rPar = (iorB * cosIn - iorA * cosT) / (iorB * cosIn + iorA * cosT + 1e-8f);
        var rPer = (iorA * cosIn - iorB * cosT) / (iorA * cosIn + iorB * cosT + 1e-8f);
        return System.Math.Clamp(0.5f * (rPar * rPar + rPer * rPer), 0f, 1f);
    }

    private static Vector3 Refract(Vector3 inDir, Vector3 normal, float iorA, float iorB)
    {
        var ratio = iorA / iorB;
        var cosIn = System.Math.Clamp(-Vector3.Dot(inDir, normal), 0f, 1f);
        var sin2 = ratio * ratio * (1f - cosIn * cosIn);
        if (sin2 > 1f)
        {
            return Vector3.Zero;
        }

        var cosT = MathF.Sqrt(MathF.Max(0f, 1f - sin2));
        return Vector3.Normalize(ratio * inDir + (ratio * cosIn - cosT) * normal);
    }

    private static Vector3 DirectLighting(in Hit hit, CompiledScene scene, GpuMaterial mat, ref Random rng)
    {
        if (scene.Lights.Length == 0)
        {
            return Vector3.Zero;
        }

        var weightSum = 0f;
        for (var i = 0; i < scene.Lights.Length; i++)
        {
            weightSum += MathF.Max(LightPower(scene.Lights[i]), 1e-4f);
        }

        var pick = (float)rng.NextDouble() * weightSum;
        var lightIndex = scene.Lights.Length - 1;
        var cumulative = 0f;
        for (var i = 0; i < scene.Lights.Length; i++)
        {
            cumulative += MathF.Max(LightPower(scene.Lights[i]), 1e-4f);
            if (pick <= cumulative)
            {
                lightIndex = i;
                break;
            }
        }

        var light = scene.Lights[lightIndex];
        var baseColor = ResolveBaseColor(mat, hit.Point);
        var metallic = mat.Model is MaterialModel.Checker or MaterialModel.Skin
            ? 0f
            : System.Math.Clamp(mat.B.X, 0f, 1f);
        var n = hit.Normal;

        Vector3 lightDir;
        float maxDist;
        float geometric;
        if (light.Kind == GpuLightKind.Directional)
        {
            lightDir = Vector3.Normalize(-light.DirectionOrPosition);
            maxDist = 1e6f;
            geometric = weightSum;
        }
        else if (light.Kind == GpuLightKind.Rect)
        {
            var su = (float)rng.NextDouble() * 2f - 1f;
            var sv = (float)rng.NextDouble() * 2f - 1f;
            var lightPos = light.DirectionOrPosition + light.U * su + light.V * sv;
            var toLight = lightPos - hit.Point;
            maxDist = toLight.Length();
            if (maxDist < 1e-4f)
            {
                return Vector3.Zero;
            }

            lightDir = toLight / maxDist;
            var lightNormal = Vector3.Normalize(Vector3.Cross(light.U, light.V));
            var cosLight = MathF.Abs(Vector3.Dot(lightNormal, -lightDir));
            if (cosLight <= 1e-5f)
            {
                return Vector3.Zero;
            }

            var rectArea = 4f * light.U.Length() * light.V.Length();
            var power = MathF.Max(LightPower(light), 1e-4f);
            var distSq = MathF.Max(maxDist * maxDist, 0.04f);
            geometric = (weightSum / power) * light.Intensity * cosLight * rectArea / distSq;
        }
        else
        {
            var toLight = light.DirectionOrPosition - hit.Point;
            maxDist = toLight.Length();
            lightDir = maxDist > 1e-4f ? toLight / maxDist : Vector3.UnitY;
            var power = MathF.Max(LightPower(light), 1e-4f);
            var distSq = MathF.Max(maxDist * maxDist, 0.04f);
            geometric = (weightSum / power) * light.Intensity / distSq;
        }

        if (!ShadowRayClear(hit.Point, lightDir, scene, maxDist))
        {
            return Vector3.Zero;
        }

        var ndotl = MathF.Max(0f, Vector3.Dot(n, lightDir));
        var diffuse = baseColor * ((1f - metallic) * ndotl / MathF.PI);
        var f0 = Vector3.Lerp(new Vector3(0.04f), baseColor, metallic);
        var specular = f0 * (metallic * ndotl);
        return (diffuse + specular) * light.Color * geometric;
    }

    private static float LightPower(GpuLight light)
    {
        if (light.Kind == GpuLightKind.Rect)
        {
            return light.Intensity * MathF.Max(4f * light.U.Length() * light.V.Length(), 1e-6f);
        }

        return light.Intensity;
    }

    private static Vector3 ClampRadiance(Vector3 radiance)
    {
        var peak = MathF.Max(radiance.X, MathF.Max(radiance.Y, radiance.Z));
        if (peak <= FireflyClamp)
        {
            return radiance;
        }

        return radiance * (FireflyClamp / peak);
    }

    private static Vector3 ResolveBaseColor(GpuMaterial mat, Vector3 hitPoint)
    {
        if (mat.Model != MaterialModel.Checker)
        {
            return new Vector3(mat.A.X, mat.A.Y, mat.A.Z);
        }

        var scale = MathF.Max(mat.B.W, 1e-3f);
        var ix = (int)MathF.Floor(hitPoint.X * scale);
        var iz = (int)MathF.Floor(hitPoint.Z * scale);
        return ((ix + iz) & 1) != 0
            ? new Vector3(mat.B.X, mat.B.Y, mat.B.Z)
            : new Vector3(mat.A.X, mat.A.Y, mat.A.Z);
    }

    private static Vector3 RandomDirection(ref Random rng)
    {
        var theta = 2f * MathF.PI * (float)rng.NextDouble();
        var rho = MathF.Sqrt(-2f * MathF.Log(MathF.Max(1e-6f, (float)rng.NextDouble())));
        var x = rho * MathF.Cos(theta);
        var y = rho * MathF.Sin(theta);
        theta = 2f * MathF.PI * (float)rng.NextDouble();
        rho = MathF.Sqrt(-2f * MathF.Log(MathF.Max(1e-6f, (float)rng.NextDouble())));
        var z = rho * MathF.Cos(theta);
        return Vector3.Normalize(new Vector3(x, y, z));
    }

    private static bool ShadowRayClear(Vector3 origin, Vector3 dir, CompiledScene scene, float maxDist)
    {
        // Emissive hit = reached a mesh light (point-light NEE proxies live inside emissive boxes).
        var ray = new Ray(origin + dir * Epsilon, dir);
        if (!IntersectScene(in ray, scene, out var hit, maxDist))
        {
            return true;
        }

        return scene.Materials[hit.MaterialIndex].Model == MaterialModel.Emissive;
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
        hit = new Hit(materialIndex, ray.PointAt(distance), normal, distance);
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
            hit = new Hit(tri.MaterialIndex, ray.PointAt(t), n, t);
        }

        return found;
    }

    private static Vector3 SampleSky(Vector3 direction, CompiledScene scene)
    {
        var ground = new Vector3(0.04f, 0.035f, 0.04f);
        var horizon = new Vector3(0.12f, 0.12f, 0.14f);
        var zenith = new Vector3(0.02f, 0.05f, 0.1f);
        var skyT = MathF.Pow(System.Math.Clamp((direction.Y + 0.01f) / 0.4f, 0f, 1f), 0.35f);
        var groundToSky = System.Math.Clamp(direction.Y * 50f + 0.5f, 0f, 1f);
        var sky = Vector3.Lerp(horizon, zenith, skyT);
        var env = Vector3.Lerp(ground, sky, groundToSky) * EnvStrength;

        var sun = Vector3.Zero;
        foreach (var light in scene.Lights)
        {
            if (light.Kind != GpuLightKind.Directional)
            {
                continue;
            }

            var dirToSun = Vector3.Normalize(-light.DirectionOrPosition);
            var sunDisc = MathF.Pow(MathF.Max(0f, Vector3.Dot(direction, dirToSun)), 500f) * 10f * light.Intensity;
            sun += light.Color * sunDisc;
        }

        if (groundToSky >= 1f)
        {
            env += sun;
        }

        return env;
    }

    private static Rgba32 ToRgba(Vector3 c)
    {
        static byte ToByte(float v)
        {
            var exposed = MathF.Min(v, FireflyClamp) * DisplayExposure;
            var gamma = MathF.Sqrt(MathF.Max(exposed, 0f));
            return (byte)System.Math.Clamp((int)(gamma * 255f), 0, 255);
        }

        return new Rgba32(ToByte(c.X), ToByte(c.Y), ToByte(c.Z));
    }

    private readonly record struct Hit(int MaterialIndex, Vector3 Point, Vector3 Normal, float Distance);
}
