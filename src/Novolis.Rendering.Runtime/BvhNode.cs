using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Runtime;

/// <summary>Binary BVH node for ray traversal.</summary>
public readonly record struct BvhNode(
    AxisAlignedBox3 Bounds,
    int TriangleOrderOffset,
    int TriangleCount,
    int LeftChild,
    int RightChild,
    bool IsLeaf);
