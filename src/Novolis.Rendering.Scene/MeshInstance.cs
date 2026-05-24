using System.Numerics;
using Novolis.Rendering.Materials;

namespace Novolis.Rendering.Scene;

/// <summary>Mesh instance in authoring space.</summary>
public sealed class MeshInstance
{
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

    public Vector3[] Vertices { get; }
    public int[] TriangleIndices { get; }
    public IMaterial Material { get; }
    public Matrix4x4 Transform { get; }
}
