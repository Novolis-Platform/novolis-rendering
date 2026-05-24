using System.Numerics;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>ILGPU-friendly BVH node (flat bounds, no external geometry types).</summary>
public readonly struct GpuBvhNode
{
    public Vector3 BoundsMin { get; init; }
    public Vector3 BoundsMax { get; init; }
    public int TriangleOrderOffset { get; init; }
    public int TriangleCount { get; init; }
    public int LeftChild { get; init; }
    public int RightChild { get; init; }
    public int IsLeaf { get; init; }

    public static GpuBvhNode From(BvhNode node) =>
        new()
        {
            BoundsMin = node.Bounds.Min,
            BoundsMax = node.Bounds.Max,
            TriangleOrderOffset = node.TriangleOrderOffset,
            TriangleCount = node.TriangleCount,
            LeftChild = node.LeftChild,
            RightChild = node.RightChild,
            IsLeaf = node.IsLeaf ? 1 : 0,
        };
}
