using System.Numerics;
using Novolis.Rendering.Materials;

namespace Novolis.Rendering.Scene;

/// <summary>Mesh instance in authoring space.</summary>
public sealed class MeshInstance
{
    /// <summary>Creates a mesh with an optional instance transform.</summary>
    /// <param name="vertices">Local-space vertex positions.</param>
    /// <param name="triangleIndices">Triangle index buffer (length multiple of three).</param>
    /// <param name="material">Authoring material.</param>
    /// <param name="transform">Instance transform applied to vertices.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="triangleIndices"/> length is invalid.</exception>
    public MeshInstance(Vector3[] vertices, int[] triangleIndices, IMaterial material, Matrix4x4 transform)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(triangleIndices);
        ArgumentNullException.ThrowIfNull(material);
        if (triangleIndices.Length % 3 != 0)
        {
            throw new ArgumentException("Triangle index count must be a multiple of 3.", nameof(triangleIndices));
        }

        Vertices = vertices;
        TriangleIndices = triangleIndices;
        Material = material;
        Transform = transform;
    }

    /// <summary>Local-space vertex positions.</summary>
    public Vector3[] Vertices { get; }

    /// <summary>Triangle corner indices.</summary>
    public int[] TriangleIndices { get; }

    /// <summary>Authoring material assigned to all triangles.</summary>
    public IMaterial Material { get; }

    /// <summary>Instance transform from local to world space.</summary>
    public Matrix4x4 Transform { get; }
}
