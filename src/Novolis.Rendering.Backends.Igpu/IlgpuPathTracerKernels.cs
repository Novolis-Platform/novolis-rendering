using System.Runtime.CompilerServices;
using ILGPU;
using ILGPU.Runtime;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>GPU path tracing kernels (iterative; ILGPU does not support recursive device methods).</summary>
internal static class IlgpuPathTracerKernels
{
    private const int MaxDepth = 4;
    private const float Epsilon = 1e-4f;
    private const int MaxLights = 8;
    private const int BvhStackCapacity = 64;
    private const float DisplayExposure = 2f;
    private const float AmbientStrength = 0.08f;

    public static void TracePixelKernel(
        Index1D index,
        int width,
        int height,
        int sampleIndex,
        IlgpuCameraParams camera,
        ArrayView<Float3> accumulation,
        ArrayView<byte> displayRgba,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuMaterial> materials,
        ArrayView<GpuLight> lights,
        int lightCount,
        ArrayView<GpuBvhNode> bvhNodes,
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
        var origin = camera.Position;
        var direction = Float3.Normalize(Float3.Add(Float3.Add(camera.Forward, Float3.Scale(camera.Right, u)), Float3.Scale(camera.Up, v)));
        var throughput = Float3.One;
        var radiance = Float3.Zero;

        for (var depth = 0; depth < MaxDepth; depth++)
        {
            if (!IntersectScene(origin, direction, triangles, bvhNodes, bvhRootIndex, triangleOrder, float.MaxValue, out var hit))
            {
                radiance = Float3.Add(radiance, Float3.Mul(throughput, SampleSky(direction)));
                break;
            }

            var mat = materials[hit.MaterialIndex];
            if (mat.Model == MaterialModel.Emissive)
            {
                radiance = Float3.Add(radiance, Float3.Mul(throughput, Float3.Scale(new Float3(mat.A.X, mat.A.Y, mat.A.Z), mat.A.W)));
                break;
            }

            radiance = Float3.Add(
                radiance,
                Float3.Mul(throughput, DirectLighting(direction, hit, mat, lights, lightCount, triangles, bvhNodes, bvhRootIndex, triangleOrder)));

            if (depth + 1 >= MaxDepth)
            {
                break;
            }

            var baseColor = new Float3(mat.A.X, mat.A.Y, mat.A.Z);
            var roughness = TracerMath.Max(mat.A.W, 0.04f);
            var metallic = mat.B.X;
            var n = hit.Normal;

            if (metallic > 0.75f)
            {
                var mirrorWeight = metallic * (1f - TracerMath.Min(roughness / 0.12f, 1f));
                if (mirrorWeight > 0.01f)
                {
                    origin = Float3.Add(hit.Point, Float3.Scale(n, Epsilon));
                    direction = Float3.Normalize(Float3.Reflect(direction, n));
                    throughput = Float3.Mul(throughput, Float3.Scale(baseColor, mirrorWeight));
                    continue;
                }
            }

            if (roughness > 0.01f && NextFloat(ref rng) > metallic)
            {
                origin = Float3.Add(hit.Point, Float3.Scale(n, Epsilon));
                direction = CosineHemisphere(n, ref rng);
                throughput = Float3.Mul(throughput, Float3.Scale(baseColor, 0.5f));
                continue;
            }

            break;
        }

        var invCount = 1f / (sampleIndex + 1);
        var blended = Float3.Add(accumulation[index], radiance);
        accumulation[index] = blended;
        WriteRgba(displayRgba, index, Float3.Scale(blended, invCount));
    }

    private static Float3 DirectLighting(
        Float3 incomingDirection,
        Hit hit,
        GpuMaterial mat,
        ArrayView<GpuLight> lights,
        int lightCount,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuBvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder)
    {
        var baseColor = new Float3(mat.A.X, mat.A.Y, mat.A.Z);
        var roughness = TracerMath.Max(mat.A.W, 0.04f);
        var metallic = mat.B.X;
        var emissionStrength = mat.B.Y;
        var emissionColor = new Float3(mat.C.X, mat.C.Y, mat.C.Z);
        var n = hit.Normal;
        var v = Float3.Normalize(Float3.Scale(incomingDirection, -1f));
        var radiance = Float3.Scale(emissionColor, emissionStrength);
        var lightsToProcess = TracerMath.Min(lightCount, MaxLights);

        for (var i = 0; i < lightsToProcess; i++)
        {
            var light = lights[i];
            var lightPos = new Float3(
                light.DirectionOrPosition.X,
                light.DirectionOrPosition.Y,
                light.DirectionOrPosition.Z);
            var lightDir = light.Kind == GpuLightKind.Directional
                ? Float3.Normalize(Float3.Scale(lightPos, -1f))
                : Float3.Normalize(Float3.Sub(lightPos, hit.Point));

            var maxDist = light.Kind == GpuLightKind.Directional
                ? 1e6f
                : Float3.Length(Float3.Sub(lightPos, hit.Point));

            if (!ShadowClear(hit.Point, hit.Normal, lightDir, maxDist, triangles, bvhNodes, bvhRootIndex, triangleOrder))
            {
                continue;
            }

            var ndotl = TracerMath.Max(0f, Float3.Dot(n, lightDir));
            var diffuse = Float3.Scale(baseColor, 1f - metallic);
            var f0 = Float3.Lerp(new Float3(0.04f, 0.04f, 0.04f), baseColor, metallic);
            var spec = GgxSpec(n, v, lightDir, roughness, f0);
            var lightColor = new Float3(light.Color.X, light.Color.Y, light.Color.Z);
            radiance = Float3.Add(radiance, Float3.Mul(Float3.Add(Float3.Scale(diffuse, ndotl), spec), Float3.Scale(lightColor, light.Intensity)));
        }

        return Float3.Add(radiance, Float3.Scale(baseColor, AmbientStrength));
    }

    private static bool IntersectScene(
        Float3 origin,
        Float3 direction,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuBvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder,
        float maxT,
        out Hit hit)
    {
        hit = default;
        if (bvhRootIndex >= 0 && bvhNodes.Length > 0)
        {
            var bestT = maxT;
            var found = false;
            var stack = new int[BvhStackCapacity];
            var stackSize = 0;
            stack[stackSize++] = bvhRootIndex;

            while (stackSize > 0)
            {
                var nodeIndex = stack[--stackSize];
                var node = bvhNodes[nodeIndex];
                if (!RaySlabIntersect(node.BoundsMin, node.BoundsMax, origin, direction, 0f, bestT))
                {
                    continue;
                }

                if (node.IsLeaf != 0)
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
                        hit = new Hit(tri.MaterialIndex, Float3.Add(origin, Float3.Scale(direction, t)), n);
                    }

                    continue;
                }

                if (stackSize + 2 <= BvhStackCapacity)
                {
                    stack[stackSize++] = node.LeftChild;
                    stack[stackSize++] = node.RightChild;
                }
            }

            return found;
        }

        return IntersectBrute(origin, direction, triangles, maxT, out hit);
    }

    private static bool IntersectBrute(
        Float3 origin,
        Float3 direction,
        ArrayView<GpuTriangle> triangles,
        float maxT,
        out Hit hit)
    {
        hit = default;
        var bestT = maxT;
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
            hit = new Hit(tri.MaterialIndex, Float3.Add(origin, Float3.Scale(direction, t)), n);
        }

        return found;
    }

    private static bool ShadowClear(
        Float3 origin,
        Float3 normal,
        Float3 direction,
        float maxDist,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuBvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder)
    {
        var shadowOrigin = Float3.Add(origin, Float3.Scale(normal, Epsilon));
        return !IntersectScene(shadowOrigin, direction, triangles, bvhNodes, bvhRootIndex, triangleOrder, maxDist, out _);
    }

    private static bool TriangleHit(
        Float3 origin,
        Float3 direction,
        GpuTriangle tri,
        float maxT,
        out float t,
        out Float3 normal)
    {
        var v0 = new Float3(tri.A.X, tri.A.Y, tri.A.Z);
        var v1 = new Float3(tri.B.X, tri.B.Y, tri.B.Z);
        var v2 = new Float3(tri.C.X, tri.C.Y, tri.C.Z);
        var edge1 = Float3.Sub(v1, v0);
        var edge2 = Float3.Sub(v2, v0);
        var pvec = Float3.Cross(direction, edge2);
        var det = Float3.Dot(edge1, pvec);
        if (TracerMath.Abs(det) < 1e-8f)
        {
            t = 0f;
            normal = default;
            return false;
        }

        var invDet = 1f / det;
        var tvec = Float3.Sub(origin, v0);
        var u = Float3.Dot(tvec, pvec) * invDet;
        if (u < 0f || u > 1f)
        {
            t = 0f;
            normal = default;
            return false;
        }

        var qvec = Float3.Cross(tvec, edge1);
        var v = Float3.Dot(direction, qvec) * invDet;
        if (v < 0f || u + v > 1f)
        {
            t = 0f;
            normal = default;
            return false;
        }

        t = Float3.Dot(edge2, qvec) * invDet;
        if (t < Epsilon || t > maxT)
        {
            normal = default;
            return false;
        }

        normal = Float3.Normalize(Float3.Cross(edge1, edge2));
        if (Float3.Dot(normal, direction) > 0f)
        {
            normal = Float3.Scale(normal, -1f);
        }

        return true;
    }

    private static bool RaySlabIntersect(
        Float3 boxMin,
        Float3 boxMax,
        Float3 origin,
        Float3 direction,
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
                var swap = t0;
                t0 = t1;
                t1 = swap;
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

    private static Float3 GgxSpec(Float3 n, Float3 v, Float3 l, float roughness, Float3 f0)
    {
        var h = Float3.Normalize(Float3.Add(v, l));
        var ndoth = TracerMath.Max(0f, Float3.Dot(n, h));
        var ndotl = TracerMath.Max(0f, Float3.Dot(n, l));
        var ndotv = TracerMath.Max(0f, Float3.Dot(n, v));
        if (ndotl <= 0f || ndotv <= 0f)
        {
            return Float3.Zero;
        }

        var a = roughness * roughness;
        var a2 = a * a;
        var denom = ndoth * ndoth * (a2 - 1f) + 1f;
        var d = a2 / (TracerMath.PI * denom * denom + 1e-7f);
        return Float3.Scale(f0, d * ndotl);
    }

    private static Float3 CosineHemisphere(Float3 normal, ref uint rng)
    {
        var u1 = NextFloat(ref rng);
        var u2 = NextFloat(ref rng);
        var r = TracerMath.Sqrt(u1);
        var phi = 2f * TracerMath.PI * u2;
        var x = r * TracerMath.Cos(phi);
        var y = r * TracerMath.Sin(phi);
        var z = TracerMath.Sqrt(TracerMath.Max(0f, 1f - u1));
        var up = TracerMath.Abs(normal.Y) < 0.999f ? Float3.UnitY : Float3.UnitX;
        var tangent = Float3.Normalize(Float3.Cross(up, normal));
        var bitangent = Float3.Cross(normal, tangent);
        return Float3.Normalize(Float3.Add(Float3.Add(Float3.Scale(tangent, x), Float3.Scale(bitangent, y)), Float3.Scale(normal, z)));
    }

    private static Float3 SampleSky(Float3 direction)
    {
        var t = TracerMath.Clamp(direction.Y * 0.5f + 0.5f, 0f, 1f);
        var low = new Float3(40f / 255f, 44f / 255f, 52f / 255f);
        var high = new Float3(120f / 255f, 168f / 255f, 220f / 255f);
        return Float3.Lerp(low, high, t);
    }

    private static void WriteRgba(ArrayView<byte> display, Index1D index, Float3 color)
    {
        var offset = index * 4;
        display[offset] = ToByte(color.X);
        display[offset + 1] = ToByte(color.Y);
        display[offset + 2] = ToByte(color.Z);
        display[offset + 3] = 255;
    }

    private static byte ToByte(float v)
    {
        var exposed = v * DisplayExposure;
        var gamma = TracerMath.Sqrt(TracerMath.Max(exposed, 0f));
        return (byte)TracerMath.Clamp((int)(gamma * 255f), 0, 255);
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

    private struct Hit
    {
        public int MaterialIndex;
        public Float3 Point;
        public Float3 Normal;

        public Hit(int materialIndex, Float3 point, Float3 normal)
        {
            MaterialIndex = materialIndex;
            Point = point;
            Normal = normal;
        }
    }
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
