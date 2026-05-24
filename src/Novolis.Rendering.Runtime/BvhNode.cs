using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Runtime;

/// <summary>Binary BVH node for ray traversal.</summary>
/// <param name="Bounds">Axis-aligned bounds enclosing child geometry.</param>
/// <param name="TriangleOrderOffset">Start index into the triangle order array for leaf nodes.</param>
/// <param name="TriangleCount">Number of triangles in this leaf (zero for internal nodes).</param>
/// <param name="LeftChild">Left child node index, or -1.</param>
/// <param name="RightChild">Right child node index, or -1.</param>
/// <param name="IsLeaf">True when this node references triangles directly.</param>
public readonly record struct BvhNode(
    AxisAlignedBox3 Bounds,
    int TriangleOrderOffset,
    int TriangleCount,
    int LeftChild,
    int RightChild,
    bool IsLeaf);
