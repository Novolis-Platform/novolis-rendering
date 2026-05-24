using System.Numerics;
using System.Runtime.CompilerServices;
using ILGPU;
using ILGPU.Runtime;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>GPU path tracing kernels (primary rays, standard materials, BVH).</summary>
internal static class IlgpuPathTracerKernels
{
    private const int MaxDepth = 4;
    private const float Epsilon = 1e-4f;
    private const int MaxLights = 8;

    public static void TracePixelKernel(
        Index1D index,
        int width,
        int height,
        int sampleIndex,
        IlgpuCameraParams camera,
        ArrayView<Vector3> accumulation,
        ArrayView<byte> displayRgba,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuMaterial> materials,
        ArrayView<GpuLight> lights,
        int lightCount,
        ArrayView<BvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder)
    {
        var x = index % width;
        var y = index / width;
        var rng = CreateRng(index, sampleIndex, width);
        var jitterX = NextFloat(ref rng);
        var jitterY = NextFloat(ref rng);
        var u = (2f * (x + jitterX) / width - 1f) * camera.TanHalfFov * camera.Aspect;
        var v = (2f * (y + jitterY) / height - 1f) * camera.TanHalfFov;
        var dir = Vector3.Normalize(camera.Forward + u * camera.Right + v * camera.Up);
        var radiance = TracePath(camera.Position, dir, 0, ref rng, triangles, materials, lights, lightCount, bvhNodes, bvhRootIndex, triangleOrder);
        var invCount = 1f / (sampleIndex + 1);
        var blended = accumulation[index] + radiance;
        accumulation[index] = blended;
        WriteRgba(displayRgba, index, blended * invCount);
    }

    private static Vector3 TracePath(
        Vector3 origin,
        Vector3 direction,
        int depth,
        ref uint rng,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuMaterial> materials,
        ArrayView<GpuLight> lights,
        int lightCount,
        ArrayView<BvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder)
    {
        if (!IntersectScene(origin, direction, triangles, bvhNodes, bvhRootIndex, triangleOrder, out var hit))
        {
            return SampleSky(direction);
        }

        var mat = materials[hit.MaterialIndex];
        if (mat.Model == MaterialModel.Emissive)
        {
            return new Vector3(mat.A.X, mat.A.Y, mat.A.Z) * mat.A.W;
        }

        return StandardShade(origin, direction, in hit, depth, ref rng, mat, triangles, materials, lights, lightCount, bvhNodes, bvhRootIndex, triangleOrder);
    }

    private static Vector3 StandardShade(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        in Hit hit,
        int depth,
        ref uint rng,
        GpuMaterial mat,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuMaterial> materials,
        ArrayView<GpuLight> lights,
        int lightCount,
        ArrayView<BvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder)
    {
        var baseColor = new Vector3(mat.A.X, mat.A.Y, mat.A.Z);
        var roughness = TracerMath.Max(mat.A.W, 0.04f);
        var metallic = mat.B.X;
        var emissionStrength = mat.B.Y;
        var emissionColor = new Vector3(mat.C.X, mat.C.Y, mat.C.Z);
        var n = hit.Normal;
        var v = Vector3.Normalize(-rayDirection);
        var radiance = emissionColor * emissionStrength;
        var lightsToProcess = TracerMath.Min(lightCount, MaxLights);

        for (var i = 0; i < lightsToProcess; i++)
        {
            var light = lights[i];
            var lightDir = light.Kind == GpuLightKind.Directional
                ? Vector3.Normalize(-light.DirectionOrPosition)
                : Vector3.Normalize(light.DirectionOrPosition - hit.Point);

            var maxDist = light.Kind == GpuLightKind.Directional
                ? 1e6f
                : (light.DirectionOrPosition - hit.Point).Length();

            if (!ShadowClear(hit.Point, lightDir, maxDist, triangles, bvhNodes, bvhRootIndex, triangleOrder))
            {
                continue;
            }

            var ndotl = TracerMath.Max(0f, Vector3.Dot(n, lightDir));
            var diffuse = baseColor * (1f - metallic);
            var spec = GgxSpec(n, v, lightDir, roughness, Vector3.One);
            radiance += (diffuse * ndotl + spec) * light.Color * light.Intensity;
        }

        radiance += baseColor * 0.03f;

        if (depth < MaxDepth && metallic > 0.75f)
        {
            var mirrorWeight = metallic * (1f - TracerMath.Min(roughness / 0.12f, 1f));
            if (mirrorWeight > 0.01f)
            {
                var reflectDir = Vector3.Reflect(rayDirection, n);
                radiance += baseColor * TracePath(
                    hit.Point + n * Epsilon,
                    Vector3.Normalize(reflectDir),
                    depth + 1,
                    ref rng,
                    triangles,
                    materials,
                    lights,
                    lightCount,
                    bvhNodes,
                    bvhRootIndex,
                    triangleOrder) * mirrorWeight;
            }
        }

        if (depth < MaxDepth && roughness > 0.01f && NextFloat(ref rng) > metallic)
        {
            var bounceDir = CosineHemisphere(n, ref rng);
            radiance += baseColor * TracePath(
                hit.Point + n * Epsilon,
                bounceDir,
                depth + 1,
                ref rng,
                triangles,
                materials,
                lights,
                lightCount,
                bvhNodes,
                bvhRootIndex,
                triangleOrder) * 0.5f;
        }

        return radiance;
    }

    private static bool IntersectScene(
        Vector3 origin,
        Vector3 direction,
        ArrayView<GpuTriangle> triangles,
        ArrayView<BvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder,
        out Hit hit)
    {
        hit = default;
        if (bvhRootIndex >= 0 && bvhNodes.Length > 0)
        {
            var bestT = float.MaxValue;
            var found = false;
            TraverseBvh(bvhRootIndex, origin, direction, triangles, bvhNodes, triangleOrder, float.MaxValue, ref bestT, ref found, ref hit);
            return found;
        }

        return IntersectBrute(origin, direction, triangles, out hit);
    }

    private static void TraverseBvh(
        int nodeIndex,
        Vector3 origin,
        Vector3 direction,
        ArrayView<GpuTriangle> triangles,
        ArrayView<BvhNode> bvhNodes,
        ArrayView<int> triangleOrder,
        float maxDistance,
        ref float bestT,
        ref bool found,
        ref Hit hit)
    {
        var node = bvhNodes[nodeIndex];
        if (!RaySlabIntersect(node.Bounds.Min, node.Bounds.Max, origin, direction, 0f, maxDistance))
        {
            return;
        }

        if (node.IsLeaf)
        {
            for (var i = 0; i < node.TriangleCount; i++)
            {
                var triIdx = triangleOrder[node.TriangleOrderOffset + i];
                var tri = triangles[triIdx];
                if (!TriangleHit(origin, direction, tri, bestT, out var t, out var n))
                {
                    continue;
                }

                found = true;
                bestT = t;
                hit = new Hit(tri.MaterialIndex, origin + direction * t, n);
            }

            return;
        }

        TraverseBvh(node.LeftChild, origin, direction, triangles, bvhNodes, triangleOrder, maxDistance, ref bestT, ref found, ref hit);
        TraverseBvh(node.RightChild, origin, direction, triangles, bvhNodes, triangleOrder, maxDistance, ref bestT, ref found, ref hit);
    }

    private static bool IntersectBrute(
        Vector3 origin,
        Vector3 direction,
        ArrayView<GpuTriangle> triangles,
        out Hit hit)
    {
        hit = default;
        var bestT = float.MaxValue;
        var found = false;
        for (var i = 0; i < triangles.Length; i++)
        {
            var tri = triangles[i];
            if (!TriangleHit(origin, direction, tri, bestT, out var t, out var n))
            {
                continue;
            }

            found = true;
            bestT = t;
            hit = new Hit(tri.MaterialIndex, origin + direction * t, n);
        }

        return found;
    }

    private static bool ShadowClear(
        Vector3 origin,
        Vector3 direction,
        float maxDist,
        ArrayView<GpuTriangle> triangles,
        ArrayView<BvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder)
    {
        return !IntersectScene(origin + direction * Epsilon, direction, triangles, bvhNodes, bvhRootIndex, triangleOrder, out _);
    }

    private static bool TriangleHit(
        Vector3 origin,
        Vector3 direction,
        GpuTriangle tri,
        float maxT,
        out float t,
        out Vector3 normal)
    {
        var v0 = new Vector3(tri.A.X, tri.A.Y, tri.A.Z);
        var v1 = new Vector3(tri.B.X, tri.B.Y, tri.B.Z);
        var v2 = new Vector3(tri.C.X, tri.C.Y, tri.C.Z);
        var edge1 = v1 - v0;
        var edge2 = v2 - v0;
        var pvec = Vector3.Cross(direction, edge2);
        var det = Vector3.Dot(edge1, pvec);
        if (TracerMath.Abs(det) < 1e-8f)
        {
            t = 0f;
            normal = default;
            return false;
        }

        var invDet = 1f / det;
        var tvec = origin - v0;
        var u = Vector3.Dot(tvec, pvec) * invDet;
        if (u < 0f || u > 1f)
        {
            t = 0f;
            normal = default;
            return false;
        }

        var qvec = Vector3.Cross(tvec, edge1);
        var v = Vector3.Dot(direction, qvec) * invDet;
        if (v < 0f || u + v > 1f)
        {
            t = 0f;
            normal = default;
            return false;
        }

        t = Vector3.Dot(edge2, qvec) * invDet;
        if (t < Epsilon || t > maxT)
        {
            normal = default;
            return false;
        }

        normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
        return true;
    }

    private static bool RaySlabIntersect(
        Vector3 boxMin,
        Vector3 boxMax,
        Vector3 origin,
        Vector3 direction,
        float minT,
        float maxT)
    {
        var tEnter = minT;
        var tExit = maxT;
        for (var axis = 0; axis < 3; axis++)
        {
            var o = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            var d = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
            var min = axis == 0 ? boxMin.X : axis == 1 ? boxMin.Y : boxMin.Z;
            var max = axis == 0 ? boxMax.X : axis == 1 ? boxMax.Y : boxMax.Z;
            if (TracerMath.Abs(d) < 1e-15f)
            {
                if (o < min || o > max)
                {
                    return false;
                }

                continue;
            }

            var invD = 1f / d;
            var t0 = (min - o) * invD;
            var t1 = (max - o) * invD;
            if (t0 > t1)
            {
                (t0, t1) = (t1, t0);
            }

            tEnter = TracerMath.Max(tEnter, t0);
            tExit = TracerMath.Min(tExit, t1);
            if (tEnter > tExit)
            {
                return false;
            }
        }

        return true;
    }

    private static Vector3 GgxSpec(Vector3 n, Vector3 v, Vector3 l, float roughness, Vector3 f0)
    {
        var h = Vector3.Normalize(v + l);
        var ndoth = TracerMath.Max(0f, Vector3.Dot(n, h));
        var ndotl = TracerMath.Max(0f, Vector3.Dot(n, l));
        var ndotv = TracerMath.Max(0f, Vector3.Dot(n, v));
        if (ndotl <= 0f || ndotv <= 0f)
        {
            return Vector3.Zero;
        }

        var a = roughness * roughness;
        var a2 = a * a;
        var denom = ndoth * ndoth * (a2 - 1f) + 1f;
        var d = a2 / (TracerMath.PI * denom * denom + 1e-7f);
        return f0 * d * ndotl;
    }

    private static Vector3 CosineHemisphere(Vector3 normal, ref uint rng)
    {
        var u1 = NextFloat(ref rng);
        var u2 = NextFloat(ref rng);
        var r = TracerMath.Sqrt(u1);
        var phi = 2f * TracerMath.PI * u2;
        var x = r * TracerMath.Cos(phi);
        var y = r * TracerMath.Sin(phi);
        var z = TracerMath.Sqrt(TracerMath.Max(0f, 1f - u1));
        var up = TracerMath.Abs(normal.Y) < 0.999f ? Vector3.UnitY : Vector3.UnitX;
        var tangent = Vector3.Normalize(Vector3.Cross(up, normal));
        var bitangent = Vector3.Cross(normal, tangent);
        return Vector3.Normalize(tangent * x + bitangent * y + normal * z);
    }

    private static Vector3 SampleSky(Vector3 direction)
    {
        var t = TracerMath.Clamp(direction.Y * 0.5f + 0.5f, 0f, 1f);
        var low = new Vector3(40f / 255f, 44f / 255f, 52f / 255f);
        var high = new Vector3(120f / 255f, 168f / 255f, 220f / 255f);
        return Vector3.Lerp(low, high, t);
    }

    private static void WriteRgba(ArrayView<byte> display, Index1D index, Vector3 color)
    {
        static byte ToByte(float v) => (byte)TracerMath.Clamp((int)(v * 255f), 0, 255);
        var offset = index * 4;
        display[offset] = ToByte(color.X);
        display[offset + 1] = ToByte(color.Y);
        display[offset + 2] = ToByte(color.Z);
        display[offset + 3] = 255;
    }

    private static uint CreateRng(Index1D index, int sampleIndex, int width) =>
        (uint)(42 + sampleIndex * 100_000 + index * 7919 + width);

    private static float NextFloat(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0xFFFFFF) / (float)0x1000000;
    }

    private readonly record struct Hit(int MaterialIndex, Vector3 Point, Vector3 Normal);
}

/// <summary>ILGPU-friendly math aliases.</summary>
internal static class TracerMath
{
    public static float PI => MathF.PI;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Abs(float v) => MathF.Abs(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(float a, float b) => MathF.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(float a, float b) => MathF.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sqrt(float v) => MathF.Sqrt(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cos(float v) => MathF.Cos(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sin(float v) => MathF.Sin(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
}
