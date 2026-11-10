using ILGPU;
using ILGPU.Runtime;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>
/// GPU path tracing kernels (iterative; ILGPU does not support recursive device methods).
/// Scatter follows Sebastian Lague-style throughput + Russian roulette + Fresnel glass,
/// kept CUDA-ICE-safe (no custom value-type ctors, no MathF.Log/Exp/Pow in-kernel).
/// </summary>
internal static class IlgpuPathTracerKernels
{
    private const int AbsoluteMaxDepth = 6;
    /// <summary>Shadow BVH is the SM tax — NEE through the second bounce (settled GI).</summary>
    private const int NeeMaxDepth = 3;
    private const float Epsilon = 1e-4f;
    private const int MaxLights = 16;
    private const int BvhStackCapacity = 64;
    private const float DisplayExposure = 1.25f;
    private const float Pi = 3.14159265f;
    private const float EnvStrength = 0.12f;
    private const float FireflyClamp = 6f;

    /// <summary>
    /// Traces <see cref="IlgpuTraceLaunch.SampleCount"/> samples for one pixel in a single launch
    /// (avoids N kernel dispatches × sync tax per interactive batch).
    /// </summary>
    public static void TracePixelKernel(
        Index1D index,
        IlgpuTraceLaunch launch,
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
        var width = launch.Width;
        var height = launch.Height;
        var startSampleIndex = launch.StartSampleIndex;
        var sampleCount = launch.SampleCount < 1 ? 1 : launch.SampleCount;
        var maxDepth = launch.MaxDepth;
        if (maxDepth < 1)
        {
            maxDepth = 1;
        }
        else if (maxDepth > AbsoluteMaxDepth)
        {
            maxDepth = AbsoluteMaxDepth;
        }

        var x = index % width;
        var y = index / width;
        var sum = startSampleIndex == 0 ? Float3.Zero : accumulation[index];
        for (var s = 0; s < sampleCount; s++)
        {
            var sampleIndex = startSampleIndex + s;
            var radiance = TraceOneSample(
                x,
                y,
                width,
                height,
                sampleIndex,
                maxDepth,
                camera,
                triangles,
                materials,
                lights,
                lightCount,
                bvhNodes,
                bvhRootIndex,
                triangleOrder);
            radiance = ClampRadiance(radiance);
            sum = sampleIndex == 0 ? radiance : Float3.Add(sum, radiance);
        }

        accumulation[index] = sum;
        var totalSamples = startSampleIndex + sampleCount;
        WriteRgba(displayRgba, index, Float3.Scale(sum, 1f / totalSamples));
    }

    private static Float3 TraceOneSample(
        int x,
        int y,
        int width,
        int height,
        int sampleIndex,
        int maxDepth,
        IlgpuCameraParams camera,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuMaterial> materials,
        ArrayView<GpuLight> lights,
        int lightCount,
        ArrayView<GpuBvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder)
    {
        var rng = CreateRng(new Index1D(x + y * width), sampleIndex, width);
        var jitterX = NextFloat(ref rng);
        var jitterY = NextFloat(ref rng);
        var u = (2f * (x + jitterX) / width - 1f) * camera.TanHalfFov * camera.Aspect;
        // Row 0 is the top of the framebuffer (matches CPU tracer + Silk blit UVs).
        var v = (1f - 2f * (y + jitterY) / height) * camera.TanHalfFov;
        var origin = camera.Position;
        var direction = Float3.Normalize(Float3.Add(Float3.Add(camera.Forward, Float3.Scale(camera.Right, u)), Float3.Scale(camera.Up, v)));
        var throughput = Float3.One;
        var radiance = Float3.Zero;

        for (var depth = 0; depth < maxDepth; depth++)
        {
            if (!IntersectScene(origin, direction, triangles, bvhNodes, bvhRootIndex, triangleOrder, float.MaxValue, out var hitMat, out var hitPoint, out var hitNormal, out var hitDist))
            {
                radiance = Float3.Add(radiance, Float3.Mul(throughput, SampleSky(direction, lights, lightCount)));
                break;
            }

            var mat = materials[hitMat];
            var frontFace = Float3.Dot(direction, hitNormal) < 0f;
            var shadingNormal = frontFace ? hitNormal : Float3.Scale(hitNormal, -1f);

            if (mat.Model == MaterialModel.Emissive)
            {
                // Specular paths (mirror/glass) must see emitters on bounce; diffuse uses NEE primarily.
                radiance = Float3.Add(
                    radiance,
                    Float3.Mul(throughput, Float3.Scale(Float3.Create(mat.A.X, mat.A.Y, mat.A.Z), mat.A.W * 0.5f)));
                break;
            }

            if (mat.Model == MaterialModel.Glass)
            {
                ScatterGlass(
                    ref origin,
                    ref direction,
                    ref throughput,
                    hitPoint,
                    hitNormal,
                    frontFace,
                    hitDist,
                    mat,
                    ref rng);
            }
            else
            {
                var baseColor = ResolveBaseColor(mat, hitPoint);
                var roughness = TracerMath.Clamp(mat.A.W, 0f, 1f);
                var metallic = mat.Model == MaterialModel.Skin || mat.Model == MaterialModel.Checker
                    ? 0f
                    : TracerMath.Clamp(mat.B.X, 0f, 1f);
                if (mat.Model == MaterialModel.Skin)
                {
                    var subsurface = TracerMath.Clamp(mat.B.X, 0f, 1f);
                    var blood = Float3.Create(mat.C.X, mat.C.Y, mat.C.Z);
                    baseColor = Float3.Lerp(baseColor, blood, subsurface * 0.35f);
                }

                // NEE for dielectrics only — mirrors/metals find lights via specular bounce.
                if (depth < NeeMaxDepth && metallic < 0.5f)
                {
                    radiance = Float3.Add(
                        radiance,
                        Float3.Mul(
                            throughput,
                            DirectLighting(hitPoint, shadingNormal, mat, lights, lightCount, triangles, materials, bvhNodes, bvhRootIndex, triangleOrder, ref rng)));
                }

                if (mat.Model == MaterialModel.Standard && mat.B.Y > 1e-5f)
                {
                    radiance = Float3.Add(
                        radiance,
                        Float3.Mul(throughput, Float3.Scale(Float3.Create(mat.C.X, mat.C.Y, mat.C.Z), mat.B.Y)));
                }

                ScatterOpaque(
                    ref origin,
                    ref direction,
                    ref throughput,
                    hitPoint,
                    shadingNormal,
                    baseColor,
                    roughness,
                    metallic,
                    ref rng);
            }

            if (Float3.MaxComponent(throughput) <= 1e-5f)
            {
                break;
            }

            // Keep specular/glass paths alive longer; dielectrics RR earlier.
            var rrDepth = mat.Model == MaterialModel.Glass ? 4 : 3;
            if (depth >= rrDepth)
            {
                var p = TracerMath.Min(Float3.MaxComponent(throughput), 0.95f);
                if (NextFloat(ref rng) >= p)
                {
                    break;
                }

                throughput = Float3.Scale(throughput, 1f / TracerMath.Max(p, 1e-4f));
            }
        }

        return radiance;
    }

    private static void ScatterOpaque(
        ref Float3 origin,
        ref Float3 direction,
        ref Float3 throughput,
        Float3 hitPoint,
        Float3 hitNormal,
        Float3 baseColor,
        float roughness,
        float metallic,
        ref uint rng)
    {
        var specularDir = Float3.Normalize(Float3.Reflect(direction, hitNormal));
        if (metallic >= 0.9f)
        {
            // Conductor: always reflect. Sharp mirrors stay sharp; roughness only cones.
            if (roughness <= 0.05f)
            {
                direction = specularDir;
            }
            else
            {
                var fuzzy = CosineHemisphere(hitNormal, ref rng);
                var t = (1f - roughness) * (1f - roughness);
                direction = Float3.Normalize(Float3.Lerp(fuzzy, specularDir, t));
            }

            throughput = Float3.Mul(throughput, baseColor);
        }
        else
        {
            var specularProbability = TracerMath.Clamp(0.04f + (1f - roughness) * 0.25f, 0.04f, 0.35f);
            var isSpecular = NextFloat(ref rng) < specularProbability;
            var diffuseDir = CosineHemisphere(hitNormal, ref rng);
            if (isSpecular)
            {
                var t = (1f - roughness) * (1f - roughness);
                direction = Float3.Normalize(Float3.Lerp(diffuseDir, specularDir, t));
                throughput = Float3.Mul(throughput, Float3.Create(0.04f, 0.04f, 0.04f));
            }
            else
            {
                direction = diffuseDir;
                throughput = Float3.Mul(throughput, baseColor);
            }
        }

        origin = Float3.Add(hitPoint, Float3.Scale(hitNormal, Epsilon));
    }

    private static void ScatterGlass(
        ref Float3 origin,
        ref Float3 direction,
        ref Float3 throughput,
        Float3 hitPoint,
        Float3 geometricNormal,
        bool frontFace,
        float hitDist,
        GpuMaterial mat,
        ref uint rng)
    {
        var tint = Float3.Create(mat.A.X, mat.A.Y, mat.A.Z);
        var roughness = TracerMath.Clamp(mat.A.W, 0f, 1f);
        var ior = TracerMath.Max(mat.B.X, 1.01f);
        var absorption = TracerMath.Max(mat.B.Y, 0f);
        var n = frontFace ? geometricNormal : Float3.Scale(geometricNormal, -1f);
        if (!frontFace && absorption > 1e-5f)
        {
            // Cheap Beer–Lambert stand-in (avoid MathF.Exp — CUDA ILGPU ICE).
            var atten = TracerMath.Max(0.2f, 1f - hitDist * absorption * 0.25f);
            throughput = Float3.Mul(throughput, Float3.Scale(tint, atten));
        }

        var iorA = frontFace ? 1f : ior;
        var iorB = frontFace ? ior : 1f;
        var reflectWeight = FresnelReflectance(direction, n, iorA, iorB);
        var reflectDir = Float3.Normalize(Float3.Reflect(direction, n));
        var refractDir = Refract(direction, n, iorA, iorB);
        var hasRefract = Float3.LengthSquared(refractDir) > 1e-8f;

        // Clear glass: do not lerp with a diffuse lobe (that made cubes look like frosted charcoal).
        if (roughness > 0.05f)
        {
            var diffuseDir = CosineHemisphere(n, ref rng);
            var blur = roughness * roughness;
            reflectDir = Float3.Normalize(Float3.Lerp(reflectDir, diffuseDir, blur));
            if (hasRefract)
            {
                refractDir = Float3.Normalize(Float3.Lerp(refractDir, Float3.Scale(diffuseDir, -1f), blur));
            }
        }

        var followReflection = !hasRefract || NextFloat(ref rng) <= reflectWeight;
        direction = followReflection ? reflectDir : refractDir;
        origin = Float3.Add(hitPoint, Float3.Scale(n, Epsilon * (followReflection ? 1f : -1f)));
        if (!followReflection && frontFace)
        {
            throughput = Float3.Mul(throughput, tint);
        }
    }

    private static Float3 ResolveBaseColor(GpuMaterial mat, Float3 hitPoint)
    {
        if (mat.Model != MaterialModel.Checker)
        {
            return Float3.Create(mat.A.X, mat.A.Y, mat.A.Z);
        }

        var scale = TracerMath.Max(mat.B.W, 1e-3f);
        var ix = (int)TracerMath.Floor(hitPoint.X * scale);
        var iz = (int)TracerMath.Floor(hitPoint.Z * scale);
        var odd = ((ix + iz) & 1) != 0;
        return odd
            ? Float3.Create(mat.B.X, mat.B.Y, mat.B.Z)
            : Float3.Create(mat.A.X, mat.A.Y, mat.A.Z);
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
        Float3 hitPoint,
        Float3 hitNormal,
        GpuMaterial mat,
        ArrayView<GpuLight> lights,
        int lightCount,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuMaterial> materials,
        ArrayView<GpuBvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder,
        ref uint rng)
    {
        var lightsToProcess = lightCount < MaxLights ? lightCount : MaxLights;
        if (lightsToProcess <= 0)
        {
            return Float3.Zero;
        }

        // Power-weighted single light — rect softbox usually wins.
        var weightSum = 0f;
        for (var i = 0; i < lightsToProcess; i++)
        {
            weightSum += TracerMath.Max(LightPower(lights[i]), 1e-4f);
        }

        var pick = NextFloat(ref rng) * weightSum;
        var lightIndex = lightsToProcess - 1;
        var cumulative = 0f;
        for (var i = 0; i < lightsToProcess; i++)
        {
            cumulative += TracerMath.Max(LightPower(lights[i]), 1e-4f);
            if (pick <= cumulative)
            {
                lightIndex = i;
                break;
            }
        }

        var light = lights[lightIndex];
        var baseColor = ResolveBaseColor(mat, hitPoint);
        var metallic = mat.Model == MaterialModel.Checker || mat.Model == MaterialModel.Skin
            ? 0f
            : TracerMath.Clamp(mat.B.X, 0f, 1f);
        var n = hitNormal;
        var lightCenter = Float3.Create(
            light.DirectionOrPosition.X,
            light.DirectionOrPosition.Y,
            light.DirectionOrPosition.Z);

        Float3 lightPos;
        Float3 lightDir;
        float maxDist;
        float geometric;
        if (light.Kind == GpuLightKind.Directional)
        {
            lightPos = lightCenter;
            lightDir = Float3.Normalize(Float3.Scale(lightPos, -1f));
            maxDist = 1e6f;
            geometric = weightSum;
        }
        else if (light.Kind == GpuLightKind.Rect)
        {
            var uAxis = Float3.Create(light.U.X, light.U.Y, light.U.Z);
            var vAxis = Float3.Create(light.V.X, light.V.Y, light.V.Z);
            var su = NextFloat(ref rng) * 2f - 1f;
            var sv = NextFloat(ref rng) * 2f - 1f;
            lightPos = Float3.Add(lightCenter, Float3.Add(Float3.Scale(uAxis, su), Float3.Scale(vAxis, sv)));
            var toLight = Float3.Sub(lightPos, hitPoint);
            maxDist = Float3.Length(toLight);
            if (maxDist < 1e-4f)
            {
                return Float3.Zero;
            }

            lightDir = Float3.Scale(toLight, 1f / maxDist);
            var lightNormal = Float3.Normalize(Float3.Cross(uAxis, vAxis));
            var cosLight = TracerMath.Abs(Float3.Dot(lightNormal, Float3.Scale(lightDir, -1f)));
            if (cosLight <= 1e-5f)
            {
                return Float3.Zero;
            }

            var rectArea = 4f * Float3.Length(uAxis) * Float3.Length(vAxis);
            // pdf_pick ∝ power; radiance stored in Intensity — scale by weightSum/power * area*cos/r².
            var power = TracerMath.Max(LightPower(light), 1e-4f);
            var distSq = TracerMath.Max(maxDist * maxDist, 0.04f);
            geometric = (weightSum / power) * light.Intensity * cosLight * rectArea / distSq;
        }
        else
        {
            lightPos = lightCenter;
            var toLight = Float3.Sub(lightPos, hitPoint);
            maxDist = Float3.Length(toLight);
            lightDir = maxDist > 1e-4f ? Float3.Scale(toLight, 1f / maxDist) : Float3.UnitY;
            var power = TracerMath.Max(LightPower(light), 1e-4f);
            var distSq = TracerMath.Max(maxDist * maxDist, 0.04f);
            geometric = (weightSum / power) * light.Intensity / distSq;
        }

        if (!ShadowClear(hitPoint, hitNormal, lightDir, maxDist, triangles, materials, bvhNodes, bvhRootIndex, triangleOrder))
        {
            return Float3.Zero;
        }

        var ndotl = TracerMath.Max(0f, Float3.Dot(n, lightDir));
        var diffuse = Float3.Scale(baseColor, (1f - metallic) * ndotl / Pi);
        var f0 = Float3.Lerp(Float3.Create(0.04f, 0.04f, 0.04f), baseColor, metallic);
        var specular = Float3.Scale(f0, metallic * ndotl);
        var brdf = Float3.Add(diffuse, specular);
        var lightColor = Float3.Create(light.Color.X, light.Color.Y, light.Color.Z);
        return Float3.Scale(Float3.Mul(brdf, lightColor), geometric);
    }

    private static float LightPower(GpuLight light)
    {
        if (light.Kind == GpuLightKind.Rect)
        {
            var area = 4f * Float3.Length(Float3.Create(light.U.X, light.U.Y, light.U.Z))
                * Float3.Length(Float3.Create(light.V.X, light.V.Y, light.V.Z));
            return light.Intensity * TracerMath.Max(area, 1e-6f);
        }

        return light.Intensity;
    }

    private static bool IntersectScene(
        Float3 origin,
        Float3 direction,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuBvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder,
        float maxT,
        out int hitMat,
        out Float3 hitPoint,
        out Float3 hitNormal,
        out float hitDist)
    {
        hitMat = 0;
        hitPoint = Float3.Zero;
        hitNormal = Float3.UnitY;
        hitDist = 0f;
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
                        hitMat = tri.MaterialIndex;
                        hitPoint = Float3.Add(origin, Float3.Scale(direction, t));
                        hitNormal = n;
                        hitDist = t;
                    }

                    continue;
                }

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

        return IntersectBrute(origin, direction, triangles, maxT, out hitMat, out hitPoint, out hitNormal, out hitDist);
    }

    private static bool IntersectBrute(
        Float3 origin,
        Float3 direction,
        ArrayView<GpuTriangle> triangles,
        float maxT,
        out int hitMat,
        out Float3 hitPoint,
        out Float3 hitNormal,
        out float hitDist)
    {
        hitMat = 0;
        hitPoint = Float3.Zero;
        hitNormal = Float3.UnitY;
        hitDist = 0f;
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
            hitMat = tri.MaterialIndex;
            hitPoint = Float3.Add(origin, Float3.Scale(direction, t));
            hitNormal = n;
            hitDist = t;
        }

        return found;
    }

    private static bool ShadowClear(
        Float3 origin,
        Float3 normal,
        Float3 direction,
        float maxDist,
        ArrayView<GpuTriangle> triangles,
        ArrayView<GpuMaterial> materials,
        ArrayView<GpuBvhNode> bvhNodes,
        int bvhRootIndex,
        ArrayView<int> triangleOrder)
    {
        // Point lights extracted from emissive meshes sit inside those meshes. Hitting the
        // emissive surface is reaching the light — not occlusion (otherwise NEE never fires → mud).
        var shadowOrigin = Float3.Add(origin, Float3.Scale(normal, Epsilon));
        if (!IntersectScene(shadowOrigin, direction, triangles, bvhNodes, bvhRootIndex, triangleOrder, maxDist, out var hitMat, out _, out _, out _))
        {
            return true;
        }

        return materials[hitMat].Model == MaterialModel.Emissive;
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
            normal = Float3.UnitY;
            return false;
        }

        var invDet = 1f / det;
        var tvec = Float3.Sub(origin, v0);
        var u = Float3.Dot(tvec, pvec) * invDet;
        if (u < 0f || u > 1f)
        {
            t = 0f;
            normal = Float3.UnitY;
            return false;
        }

        var qvec = Float3.Cross(tvec, edge1);
        var v = Float3.Dot(direction, qvec) * invDet;
        if (v < 0f || u + v > 1f)
        {
            t = 0f;
            normal = Float3.UnitY;
            return false;
        }

        t = Float3.Dot(edge2, qvec) * invDet;
        if (t < Epsilon || t > maxT)
        {
            normal = Float3.UnitY;
            return false;
        }

        // Geometric normal (not forced to face the ray) — glass needs front-face for IOR.
        normal = Float3.Normalize(Float3.Cross(edge1, edge2));
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

    private static Float3 CosineHemisphere(Float3 normal, ref uint rng)
    {
        var u1 = NextFloat(ref rng);
        var u2 = NextFloat(ref rng);
        var r = TracerMath.Sqrt(u1);
        var phi = 2f * Pi * u2;
        var x = r * TracerMath.Cos(phi);
        var y = r * TracerMath.Sin(phi);
        var z = TracerMath.Sqrt(TracerMath.Max(0f, 1f - u1));
        var up = TracerMath.Abs(normal.Y) < 0.999f ? Float3.UnitY : Float3.UnitX;
        var tangent = Float3.Normalize(Float3.Cross(up, normal));
        var bitangent = Float3.Cross(normal, tangent);
        return Float3.Normalize(Float3.Add(Float3.Add(Float3.Scale(tangent, x), Float3.Scale(bitangent, y)), Float3.Scale(normal, z)));
    }

    private static Float3 SampleSky(Float3 direction, ArrayView<GpuLight> lights, int lightCount)
    {
        // Dim env: open-face dioramas otherwise get HDR sky noise that never cleans at interactive spp.
        var ground = Float3.Create(0.04f, 0.035f, 0.04f);
        var horizon = Float3.Create(0.12f, 0.12f, 0.14f);
        var zenith = Float3.Create(0.02f, 0.05f, 0.1f);
        // Avoid MathF.Pow — raise with multiplies for a soft curve.
        var t = TracerMath.Clamp((direction.Y + 0.01f) / 0.4f, 0f, 1f);
        var skyT = t * t; // ~pow(t, 2) stand-in for Lague's 0.35 curve
        var groundToSky = TracerMath.Clamp(direction.Y * 50f + 0.5f, 0f, 1f);
        var sky = Float3.Lerp(horizon, zenith, skyT);
        var env = Float3.Scale(Float3.Lerp(ground, sky, groundToSky), EnvStrength);

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
            var d = TracerMath.Max(0f, Float3.Dot(direction, dirToSun));
            // Soft sun disc without Pow(d, 500): high even power via squaring.
            var d2 = d * d;
            var d4 = d2 * d2;
            var d8 = d4 * d4;
            var d16 = d8 * d8;
            var d32 = d16 * d16;
            var sunDisc = d32 * d16 * 8f * light.Intensity; // ~d^48 * 8
            sun = Float3.Add(sun, Float3.Scale(Float3.Create(light.Color.X, light.Color.Y, light.Color.Z), sunDisc));
        }

        if (groundToSky >= 1f)
        {
            env = Float3.Add(env, sun);
        }

        return env;
    }

    private static Float3 ClampRadiance(Float3 radiance)
    {
        var peak = Float3.MaxComponent(radiance);
        if (peak <= FireflyClamp)
        {
            return radiance;
        }

        return Float3.Scale(radiance, FireflyClamp / peak);
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
        var exposed = TracerMath.Min(v, FireflyClamp) * DisplayExposure;
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
}
