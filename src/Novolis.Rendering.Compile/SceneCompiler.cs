using System.Collections.Immutable;
using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Scene;

namespace Novolis.Rendering.Compile;

/// <summary>Compiles authoring scenes into flat runtime structures.</summary>
public static class SceneCompiler
{
    /// <summary>
    /// Caps extracted + authored lights. Must stay ≤ kernel <c>MaxLights</c>
    /// (ILGPU DirectLighting loop).
    /// </summary>
    public const int MaxCompiledLights = 16;

    /// <summary>
    /// Scales emissive mesh area×strength into a point-light intensity so DirectLighting
    /// can next-event-estimate glowing props (without this, only lucky bounces light the room → mud).
    /// </summary>
    private const float EmissivePointLightScale = 1.1f;

    /// <summary>Compiles meshes, materials, lights, and a BVH into a <see cref="CompiledScene"/>.</summary>
    /// <param name="scene">Authoring scene graph.</param>
    /// <returns>Flat runtime scene for backends.</returns>
    public static CompiledScene Compile(Novolis.Rendering.Scene.Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var triangles = new List<GpuTriangle>();
        var materials = new List<GpuMaterial>();
        var materialMap = new Dictionary<IMaterial, int>();
        var lights = new List<GpuLight>(scene.Lights.Select(ToGpuLight));

        var allVertices = new List<Vector3>();
        var allIndices = new List<int>();
        var triangleMaterialIndices = new List<int>();

        foreach (var mesh in scene.Meshes)
        {
            if (!materialMap.TryGetValue(mesh.Material, out var materialIndex))
            {
                materialIndex = materials.Count;
                materials.Add(MaterialCompiler.Compile(mesh.Material));
                materialMap[mesh.Material] = materialIndex;
            }

            var baseVertex = allVertices.Count;
            for (var i = 0; i < mesh.Vertices.Length; i++)
            {
                allVertices.Add(Vector3.Transform(mesh.Vertices[i], mesh.Transform));
            }

            var meshTriStart = triangles.Count;
            for (var t = 0; t < mesh.TriangleIndices.Length; t += 3)
            {
                var i0 = baseVertex + mesh.TriangleIndices[t];
                var i1 = baseVertex + mesh.TriangleIndices[t + 1];
                var i2 = baseVertex + mesh.TriangleIndices[t + 2];
                allIndices.Add(i0);
                allIndices.Add(i1);
                allIndices.Add(i2);
                triangleMaterialIndices.Add(materialIndex);

                var v0 = allVertices[i0];
                var v1 = allVertices[i1];
                var v2 = allVertices[i2];
                triangles.Add(new GpuTriangle(
                    new Vector4(v0, 0f),
                    new Vector4(v1, 0f),
                    new Vector4(v2, 0f),
                    materialIndex));
            }

            TryAddEmissivePointLight(lights, mesh.Material, triangles, meshTriStart, triangles.Count);
        }

        TrimLights(lights);

        if (triangles.Count == 0)
        {
            return CompiledScene.Empty with { Lights = lights.ToImmutableArray(), Materials = materials.ToImmutableArray() };
        }

        var flatIndices = new int[triangleMaterialIndices.Count * 3];
        for (var tri = 0; tri < triangleMaterialIndices.Count; tri++)
        {
            flatIndices[tri * 3] = tri * 3;
            flatIndices[tri * 3 + 1] = tri * 3 + 1;
            flatIndices[tri * 3 + 2] = tri * 3 + 2;
        }

        var expandedVerts = new Vector3[triangles.Count * 3];
        for (var tri = 0; tri < triangles.Count; tri++)
        {
            var gt = triangles[tri];
            expandedVerts[tri * 3] = new Vector3(gt.A.X, gt.A.Y, gt.A.Z);
            expandedVerts[tri * 3 + 1] = new Vector3(gt.B.X, gt.B.Y, gt.B.Z);
            expandedVerts[tri * 3 + 2] = new Vector3(gt.C.X, gt.C.Y, gt.C.Z);
        }

        var bvh = TriangleBvhBuilder.Build(expandedVerts, flatIndices);
        return new CompiledScene
        {
            Triangles = triangles.ToImmutableArray(),
            Materials = materials.ToImmutableArray(),
            Lights = lights.ToImmutableArray(),
            BvhNodes = bvh.Nodes.ToImmutableArray(),
            TriangleOrder = bvh.TriangleOrder.ToImmutableArray(),
            BvhRootIndex = bvh.RootIndex,
        };
    }

    private static void TryAddEmissivePointLight(
        List<GpuLight> lights,
        IMaterial material,
        List<GpuTriangle> triangles,
        int triStart,
        int triEnd)
    {
        if (material is not EmissiveMaterial emissive || triEnd <= triStart)
        {
            return;
        }

        var area = 0f;
        var weightedCentroid = Vector3.Zero;
        for (var i = triStart; i < triEnd; i++)
        {
            var t = triangles[i];
            var a = new Vector3(t.A.X, t.A.Y, t.A.Z);
            var b = new Vector3(t.B.X, t.B.Y, t.B.Z);
            var c = new Vector3(t.C.X, t.C.Y, t.C.Z);
            var triArea = Vector3.Cross(b - a, c - a).Length() * 0.5f;
            if (triArea <= 1e-8f)
            {
                continue;
            }

            area += triArea;
            weightedCentroid += (a + b + c) * (triArea / 3f);
        }

        if (area <= 1e-6f)
        {
            return;
        }

        var centroid = weightedCentroid / area;
        var intensity = MathF.Max(emissive.EmissionStrength, 0f) * area * EmissivePointLightScale;
        if (intensity <= 1e-5f)
        {
            return;
        }

        lights.Add(new GpuLight(GpuLightKind.Point, centroid, emissive.EmissionColor, intensity));
    }

    private static void TrimLights(List<GpuLight> lights)
    {
        if (lights.Count <= MaxCompiledLights)
        {
            return;
        }

        // Prefer keeping authored + brightest emissive proxies.
        lights.Sort(static (a, b) => b.Intensity.CompareTo(a.Intensity));
        lights.RemoveRange(MaxCompiledLights, lights.Count - MaxCompiledLights);
    }

    private static GpuLight ToGpuLight(LightDefinition light) =>
        new(
            light.Kind == LightKind.Directional ? GpuLightKind.Directional : GpuLightKind.Point,
            light.DirectionOrPosition,
            light.Color,
            light.Intensity);
}
