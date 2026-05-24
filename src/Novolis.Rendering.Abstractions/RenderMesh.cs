using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Abstractions;

/// <summary>Indexed triangle soup for ray intersection (host-neutral scene geometry).</summary>
[Obsolete("Use Novolis.Rendering.Scene.MeshInstance and SceneCompiler.")]
public sealed class RenderMesh
{
    /// <summary>Creates a mesh from indexed vertices and a flat shading color.</summary>
    /// <param name="vertices">Vertex positions.</param>
    /// <param name="triangleIndices">Triangle list (length must be a multiple of three).</param>
    /// <param name="color">Per-triangle albedo for legacy tracers.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="triangleIndices"/> length is not divisible by three.</exception>
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

    /// <summary>Vertex positions referenced by <see cref="TriangleIndices"/>.</summary>
    public Vector3[] Vertices { get; }

    /// <summary>Triangle corner indices (triplets into <see cref="Vertices"/>).</summary>
    public int[] TriangleIndices { get; }

    /// <summary>Number of triangles (<c>TriangleIndices.Length / 3</c>).</summary>
    public int TriangleCount { get; }

    /// <summary>Flat shading color for legacy CPU tracers.</summary>
    public Rgba32 Color { get; }

    /// <summary>Resolves triangle corner positions for the given triangle index.</summary>
    /// <param name="triangleIndex">Zero-based triangle index.</param>
    /// <param name="v0">First corner position.</param>
    /// <param name="v1">Second corner position.</param>
    /// <param name="v2">Third corner position.</param>
    public void GetTriangle(int triangleIndex, out Vector3 v0, out Vector3 v1, out Vector3 v2)
    {
        var i = triangleIndex * 3;
        v0 = Vertices[TriangleIndices[i]];
        v1 = Vertices[TriangleIndices[i + 1]];
        v2 = Vertices[TriangleIndices[i + 2]];
    }
}
