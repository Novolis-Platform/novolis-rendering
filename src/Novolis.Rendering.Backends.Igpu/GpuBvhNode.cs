using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>ILGPU-friendly BVH node (flat bounds, no external geometry types).</summary>
public struct GpuBvhNode
{
    public Float3 BoundsMin;
    public Float3 BoundsMax;
    public int TriangleOrderOffset;
    public int TriangleCount;
    public int LeftChild;
    public int RightChild;
    public int IsLeaf;

    public static GpuBvhNode From(BvhNode node) =>
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
