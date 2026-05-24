using System.Numerics;
using System.Runtime.InteropServices;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>std430-compatible BVH node for Vulkan compute (matches <c>path_trace.comp</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VulkanGpuBvhNode
{
    public Vector3 BoundsMin;
    public float Pad0;
    public Vector3 BoundsMax;
    public float Pad1;
    public int TriangleOrderOffset;
    public int TriangleCount;
    public int LeftChild;
    public int RightChild;
    public int IsLeaf;

    public static VulkanGpuBvhNode From(TriangleBvhNode node) =>
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
