using Novolis.Math.Geometry;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>ILGPU-friendly BVH node (flat bounds, no external geometry types).</summary>
public struct GpuBvhNode
{
    /// <summary>Minimum corner of the node bounds.</summary>
    public Float3 BoundsMin;

    /// <summary>Maximum corner of the node bounds.</summary>
    public Float3 BoundsMax;

    /// <summary>Start index into the triangle order array for leaf nodes.</summary>
    public int TriangleOrderOffset;

    /// <summary>Number of triangles in this leaf.</summary>
    public int TriangleCount;

    /// <summary>Left child node index.</summary>
    public int LeftChild;

    /// <summary>Right child node index.</summary>
    public int RightChild;

    /// <summary>1 when leaf, 0 when internal.</summary>
    public int IsLeaf;

    /// <summary>Converts a <see cref="TriangleBvhNode"/> into an ILGPU layout.</summary>
    /// <param name="node">Runtime BVH node.</param>
    /// <returns>Blittable GPU node.</returns>
    public static GpuBvhNode From(TriangleBvhNode node) =>
        new()
        {
            BoundsMin = Float3.From(node.Bounds.Min),
            BoundsMax = Float3.From(node.Bounds.Max),
            TriangleOrderOffset = node.TriangleOrderOffset,
            TriangleCount = node.TriangleCount,
            LeftChild = node.LeftChild,
            RightChild = node.RightChild,
            IsLeaf = node.IsLeaf ? 1 : 0,
        };
}
