using System.Runtime.CompilerServices;
using ILGPU;
using ILGPU.Runtime;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>
/// GPU path tracing kernels (iterative; ILGPU does not support recursive device methods).
/// Bounce math follows Sebastian Lague / demofox-style throughput + Russian roulette + Fresnel glass.
/// </summary>
internal static class IlgpuPathTracerKernels
{
    private const int MaxDepth = 5;
    private const float Epsilon = 1e-4f;
    private const int MaxLights = 8;
    private const int BvhStackCapacity = 64;
    private const float DisplayExposure = 1.6f;
    private const float Pi = 3.14159265f;

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
        // Row 0 is the top of the framebuffer (matches CPU tracer + Silk blit UVs).
        var v = (1f - 2f * (y + jitterY) / height) * camera.TanHalfFov;
        var origin = camera.Position;
        var direction = Float3.Normalize(Float3.Add(Float3.Add(camera.Forward, Float3.Scale(camera.Right, u)), Float3.Scale(camera.Up, v)));
        var throughput = Float3.One;
        var radiance = Float3.Zero;

        for (var depth = 0; depth < MaxDepth; depth++)
        {
            if (!IntersectScene(origin, direction, triangles, bvhNodes, bvhRootIndex, triangleOrder, float.MaxValue, out var hit))
            {
                radiance = Float3.Add(radiance, Float3.Mul(throughput, SampleSky(direction, lights, lightCount)));
                break;
            }

            var mat = materials[hit.MaterialIndex];
            if (mat.Model == MaterialModel.Emissive)
            {
                radiance = Float3.Add(
                    radiance,
                    Float3.Mul(throughput, Float3.Scale(Float3.Create(mat.A.X, mat.A.Y, mat.A.Z), mat.A.W)));
                break;
            }

            if (mat.Model == MaterialModel.Glass)
            {
                if (!ScatterGlass(ref origin, ref direction, ref throughput, hit, mat, ref rng))
                {
                    break;
                }
            }
            else
            {
                // Explicit directional/point lights (studio scenes) + Lague-style bounce.
                radiance = Float3.Add(
                    radiance,
                    Float3.Mul(
                        throughput,
                        DirectLighting(direction, hit, mat, lights, lightCount, triangles, bvhNodes, bvhRootIndex, triangleOrder)));

                if (mat.B.Y > 1e-5f)
                {
                    radiance = Float3.Add(
                        radiance,
                        Float3.Mul(throughput, Float3.Scale(Float3.Create(mat.C.X, mat.C.Y, mat.C.Z), mat.B.Y)));
                }

                if (!ScatterStandard(ref origin, ref direction, ref throughput, hit, mat, ref rng))
                {
                    break;
                }
            }

            // Russian roulette (after a couple of bounces) — Lague / demofox.
            if (depth >= 2)
            {
                var p = TracerMath.Max(throughput.X, TracerMath.Max(throughput.Y, throughput.Z));
                p = TracerMath.Min(p, 0.95f);
                if (NextFloat(ref rng) >= p)
                {
                    break;
                }

                throughput = Float3.Scale(throughput, 1f / TracerMath.Max(p, 1e-4f));
            }
        }

        var invCount = 1f / (sampleIndex + 1);
        var blended = Float3.Add(accumulation[index], radiance);
        accumulation[index] = blended;
        WriteRgba(displayRgba, index, Float3.Scale(blended, invCount));
    }

    private static bool ScatterStandard(
        ref Float3 origin,
        ref Float3 direction,
        ref Float3 throughput,
        Hit hit,
        GpuMaterial mat,
        ref uint rng)
    {
        var baseColor = Float3.Create(mat.A.X, mat.A.Y, mat.A.Z);
        var roughness = TracerMath.Max(mat.A.W, 0.04f);
        var metallic = TracerMath.Clamp(mat.B.X, 0f, 1f);
        var smoothness = 1f - roughness;
        var specularProbability = TracerMath.Clamp(metallic * 0.85f + 0.05f, 0.05f, 1f);
        var n = hit.Normal;

        var isSpecular = NextFloat(ref rng) < specularProbability;
        var diffuseDir = Float3.Normalize(Float3.Add(n, RandomDirection(ref rng)));
        if (Float3.Dot(diffuseDir, n) <= 0f)
        {
            diffuseDir = Float3.Normalize(Float3.Add(n, Float3.Scale(RandomDirection(ref rng), 0.5f)));
        }

        var specularDir = Float3.Normalize(Float3.Reflect(direction, n));
        var bounceDir = isSpecular
            ? Float3.Normalize(Float3.Lerp(diffuseDir, specularDir, smoothness))
            : diffuseDir;

        origin = Float3.Add(hit.Point, Float3.Scale(n, Epsilon));
        direction = bounceDir;

        // Specular lobes keep specular tint (metal base); diffuse uses albedo.
        var surface = isSpecular
            ? Float3.Lerp(Float3.Create(0.04f, 0.04f, 0.04f), baseColor, metallic)
            : Float3.Scale(baseColor, 1f - metallic * 0.85f);
        throughput = Float3.Mul(throughput, surface);
        return Float3.MaxComponent(throughput) > 1e-5f;
    }

    private static bool ScatterGlass(
        ref Float3 origin,
        ref Float3 direction,
        ref Float3 throughput,
        Hit hit,
        GpuMaterial mat,
        ref uint rng)
    {
        var tint = Float3.Create(mat.A.X, mat.A.Y, mat.A.Z);
        var roughness = TracerMath.Clamp(mat.A.W, 0f, 1f);
        var ior = TracerMath.Max(mat.B.X, 1.01f);
        var absorption = TracerMath.Max(mat.B.Y, 0f);
        var n = hit.Normal;
        var entering = Float3.Dot(direction, n) < 0f;
        if (!entering)
        {
            n = Float3.Scale(n, -1f);
            // Beer–Lambert while leaving the volume (hit distance ≈ path through medium).
            if (absorption > 1e-5f)
            {
                var atten = TracerMath.Exp(-hit.Distance * absorption);
                throughput = Float3.Mul(throughput, Float3.Scale(tint, atten));
            }
        }

        var iorA = entering ? 1f : ior;
        var iorB = entering ? ior : 1f;
        var reflectWeight = FresnelReflectance(direction, n, iorA, iorB);
        var reflectDir = Float3.Normalize(Float3.Reflect(direction, n));
        var refractDir = Refract(direction, n, iorA, iorB);
        var hasRefract = Float3.LengthSquared(refractDir) > 1e-8f;

        var diffuseDir = Float3.Normalize(Float3.Add(n, RandomDirection(ref rng)));
        reflectDir = Float3.Normalize(Float3.Lerp(diffuseDir, reflectDir, 1f - roughness * 0.85f));
        if (hasRefract)
        {
            refractDir = Float3.Normalize(Float3.Lerp(Float3.Scale(diffuseDir, -1f), refractDir, 1f - roughness));
        }

        var followReflection = !hasRefract || NextFloat(ref rng) <= reflectWeight;
        direction = followReflection ? reflectDir : refractDir;
        origin = Float3.Add(hit.Point, Float3.Scale(n, Epsilon * (followReflection ? 1f : -1f)));
        if (!followReflection)
        {
            throughput = Float3.Mul(throughput, tint);
        }

        return Float3.MaxComponent(throughput) > 1e-5f;
    }

    private static float FresnelReflectance(Float3 inDir, Float3 normal, float iorA, float iorB)
    {
        var ratio = iorA / iorB;
        var cosIn = TracerMath.Min(1f, TracerMath.Max(0f, -Float3.Dot(inDir, normal)));
        var sin2 = ratio * ratio * (1f - cosIn * cosIn);
        if (sin2 >= 1f)
        {
            return 1f;
        }

        var cosT = TracerMath.Sqrt(TracerMath.Max(0f, 1f - sin2));
        var rPar = (iorB * cosIn - iorA * cosT) / (iorB * cosIn + iorA * cosT + 1e-8f);
        var rPer = (iorA * cosIn - iorB * cosT) / (iorA * cosIn + iorB * cosT + 1e-8f);
        return TracerMath.Clamp(0.5f * (rPar * rPar + rPer * rPer), 0f, 1f);
    }

    private static Float3 Refract(Float3 inDir, Float3 normal, float iorA, float iorB)
    {
        var ratio = iorA / iorB;
        var cosIn = TracerMath.Min(1f, TracerMath.Max(0f, -Float3.Dot(inDir, normal)));
        var sin2 = ratio * ratio * (1f - cosIn * cosIn);
        if (sin2 > 1f)
        {
            return Float3.Zero;
        }

        var cosT = TracerMath.Sqrt(TracerMath.Max(0f, 1f - sin2));
        return Float3.Normalize(Float3.Add(Float3.Scale(inDir, ratio), Float3.Scale(normal, ratio * cosIn - cosT)));
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
        var baseColor = Float3.Create(mat.A.X, mat.A.Y, mat.A.Z);
        var roughness = TracerMath.Max(mat.A.W, 0.04f);
        var metallic = mat.B.X;
        var n = hit.Normal;
        var v = Float3.Normalize(Float3.Scale(incomingDirection, -1f));
        var radiance = Float3.Zero;
        var lightsToProcess = TracerMath.Min(lightCount, MaxLights);

        for (var i = 0; i < lightsToProcess; i++)
        {
            var light = lights[i];
            var lightPos = Float3.Create(
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
            var f0 = Float3.Lerp(Float3.Create(0.04f, 0.04f, 0.04f), baseColor, metallic);
            var spec = GgxSpec(n, v, lightDir, roughness, f0);
            var lightColor = Float3.Create(light.Color.X, light.Color.Y, light.Color.Z);
            radiance = Float3.Add(
                radiance,
                Float3.Mul(Float3.Add(Float3.Scale(diffuse, ndotl), spec), Float3.Scale(lightColor, light.Intensity)));
        }

        return radiance;
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
                        hit = new Hit(tri.MaterialIndex, Float3.Add(origin, Float3.Scale(direction, t)), n, t);
                    }

                    continue;
                }

                // Near child last so it is visited first (Lague BVH episode).
                if (stackSize + 2 <= BvhStackCapacity)
                {
                    var left = bvhNodes[node.LeftChild];
                    var right = bvhNodes[node.RightChild];
                    var dstL = RaySlabEnter(left.BoundsMin, left.BoundsMax, origin, direction);
                    var dstR = RaySlabEnter(right.BoundsMin, right.BoundsMax, origin, direction);
                    if (dstL <= dstR)
                    {
                        if (dstR < bestT)
                        {
                            stack[stackSize++] = node.RightChild;
                        }

                        if (dstL < bestT)
                        {
                            stack[stackSize++] = node.LeftChild;
                        }
                    }
                    else
                    {
                        if (dstL < bestT)
                        {
                            stack[stackSize++] = node.LeftChild;
                        }

                        if (dstR < bestT)
                        {
                            stack[stackSize++] = node.RightChild;
                        }
                    }
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
            hit = new Hit(tri.MaterialIndex, Float3.Add(origin, Float3.Scale(direction, t)), n, t);
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
        var v0 = Float3.Create(tri.A.X, tri.A.Y, tri.A.Z);
        var v1 = Float3.Create(tri.B.X, tri.B.Y, tri.B.Z);
        var v2 = Float3.Create(tri.C.X, tri.C.Y, tri.C.Z);
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

    private static float RaySlabEnter(Float3 boxMin, Float3 boxMax, Float3 origin, Float3 direction)
    {
        var tEnter = 0f;
        var tExit = float.MaxValue;
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
                    return float.MaxValue;
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
                return float.MaxValue;
            }
        }

        return tEnter > 0f ? tEnter : 0f;
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
        var d = a2 / (Pi * denom * denom + 1e-7f);
        return Float3.Scale(f0, d * ndotl);
    }

    private static Float3 RandomDirection(ref uint rng)
    {
        // Box–Muller → approximate isotropic direction (Lague).
        var theta = 2f * Pi * NextFloat(ref rng);
        var rho = TracerMath.Sqrt(-2f * TracerMath.Log(TracerMath.Max(1e-6f, NextFloat(ref rng))));
        var x = rho * TracerMath.Cos(theta);
        var y = rho * TracerMath.Sin(theta);
        theta = 2f * Pi * NextFloat(ref rng);
        rho = TracerMath.Sqrt(-2f * TracerMath.Log(TracerMath.Max(1e-6f, NextFloat(ref rng))));
        var z = rho * TracerMath.Cos(theta);
        return Float3.Normalize(Float3.Create(x, y, z));
    }

    private static Float3 SampleSky(Float3 direction, ArrayView<GpuLight> lights, int lightCount)
    {
        var ground = Float3.Create(0.35f, 0.3f, 0.35f);
        var horizon = Float3.Create(1f, 1f, 1f);
        var zenith = Float3.Create(0.08f, 0.37f, 0.73f);
        var skyT = TracerMath.Pow(TracerMath.Clamp((direction.Y + 0.01f) / 0.4f, 0f, 1f), 0.35f);
        var groundToSky = TracerMath.Clamp(direction.Y * 50f + 0.5f, 0f, 1f);
        var sky = Float3.Lerp(horizon, zenith, skyT);
        var env = Float3.Lerp(ground, sky, groundToSky);

        var sun = Float3.Zero;
        var count = TracerMath.Min(lightCount, MaxLights);
        for (var i = 0; i < count; i++)
        {
            var light = lights[i];
            if (light.Kind != GpuLightKind.Directional)
            {
                continue;
            }

            var dirToSun = Float3.Normalize(Float3.Scale(
                Float3.Create(light.DirectionOrPosition.X, light.DirectionOrPosition.Y, light.DirectionOrPosition.Z),
                -1f));
            var focus = 500f;
            var sunDisc = TracerMath.Pow(TracerMath.Max(0f, Float3.Dot(direction, dirToSun)), focus) * 10f * light.Intensity;
            sun = Float3.Add(sun, Float3.Scale(Float3.Create(light.Color.X, light.Color.Y, light.Color.Z), sunDisc));
        }

        if (groundToSky >= 1f)
        {
            env = Float3.Add(env, sun);
        }

        return env;
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
        (uint)(index + sampleIndex * 719393 + width * 97 + 42);

    private static float NextFloat(ref uint state)
    {
        // PCG (Lague / pcg-random.org).
        state = state * 747796405u + 2891336453u;
        var result = ((state >> (int)((state >> 28) + 4)) ^ state) * 277803737u;
        result = (result >> 22) ^ result;
        return (result & 0xFFFFFF) / (float)0x1000000;
    }

    private struct Hit
    {
        public int MaterialIndex;
        public Float3 Point;
        public Float3 Normal;
        public float Distance;

        public Hit(int materialIndex, Float3 point, Float3 normal, float distance)
        {
            MaterialIndex = materialIndex;
            Point = point;
            Normal = normal;
            Distance = distance;
        }
    }
}
