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
    /// <summary>Compiles meshes, materials, lights, and a BVH into a <see cref="CompiledScene"/>.</summary>
    /// <param name="scene">Authoring scene graph.</param>
    /// <returns>Flat runtime scene for backends.</returns>
    public static CompiledScene Compile(Novolis.Rendering.Scene.Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var triangles = new List<GpuTriangle>();
        var materials = new List<GpuMaterial>();
        var materialMap = new Dictionary<IMaterial, int>();
        var lights = scene.Lights.Select(ToGpuLight).ToImmutableArray();

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
        }

        if (triangles.Count == 0)
        {
            return CompiledScene.Empty with { Lights = lights, Materials = materials.ToImmutableArray() };
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
        var bvhNodes = bvh.Nodes.Select(n => new BvhNode(
            n.Bounds,
            n.TriangleOrderOffset,
            n.TriangleCount,
            n.LeftChild,
            n.RightChild,
            n.IsLeaf)).ToImmutableArray();

        return new CompiledScene
        {
            Triangles = triangles.ToImmutableArray(),
            Materials = materials.ToImmutableArray(),
            Lights = lights,
            BvhNodes = bvhNodes,
            TriangleOrder = bvh.TriangleOrder.ToImmutableArray(),
            BvhRootIndex = bvh.RootIndex,
        };
    }

    private static GpuLight ToGpuLight(LightDefinition light) =>
        new(
            light.Kind == LightKind.Directional ? GpuLightKind.Directional : GpuLightKind.Point,
            light.DirectionOrPosition,
            light.Color,
            light.Intensity);
}
