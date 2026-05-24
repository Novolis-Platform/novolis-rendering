using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Abstractions;

/// <summary>Indexed triangle soup for ray intersection (host-neutral scene geometry).</summary>
[Obsolete("Use Novolis.Rendering.Scene.MeshInstance and SceneCompiler.")]
public sealed class RenderMesh
{
    public RenderMesh(Vector3[] vertices, int[] triangleIndices, Rgba32 color)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(triangleIndices);
        if (triangleIndices.Length % 3 != 0)
        {
            throw new ArgumentException("Triangle index count must be a multiple of 3.", nameof(triangleIndices));
        }

        Vertices = vertices;
        TriangleIndices = triangleIndices;
        Color = color;
        TriangleCount = triangleIndices.Length / 3;
    }

    public Vector3[] Vertices { get; }

    public int[] TriangleIndices { get; }

    public int TriangleCount { get; }

    public Rgba32 Color { get; }

    public void GetTriangle(int triangleIndex, out Vector3 v0, out Vector3 v1, out Vector3 v2)
    {
        var i = triangleIndex * 3;
        v0 = Vertices[TriangleIndices[i]];
        v1 = Vertices[TriangleIndices[i + 1]];
        v2 = Vertices[TriangleIndices[i + 2]];
    }
}
