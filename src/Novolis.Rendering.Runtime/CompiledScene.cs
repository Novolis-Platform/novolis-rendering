using System.Collections.Immutable;

namespace Novolis.Rendering.Runtime;

/// <summary>Flat runtime scene consumed by ray tracing backends.</summary>
public sealed record CompiledScene
{
    /// <summary>World-space triangles with material indices.</summary>
    public required ImmutableArray<GpuTriangle> Triangles { get; init; }

    /// <summary>Flattened material table referenced by triangles.</summary>
    public required ImmutableArray<GpuMaterial> Materials { get; init; }

    /// <summary>Runtime lights for shading.</summary>
    public required ImmutableArray<GpuLight> Lights { get; init; }

    /// <summary>Binary BVH nodes for acceleration.</summary>
    public required ImmutableArray<BvhNode> BvhNodes { get; init; }

    /// <summary>Triangle permutation order used during BVH traversal.</summary>
    public required ImmutableArray<int> TriangleOrder { get; init; }

    /// <summary>Root node index into <see cref="BvhNodes"/>, or -1 when empty.</summary>
    public int BvhRootIndex { get; init; } = -1;

    /// <summary>Empty scene sentinel used before the first compile.</summary>
    public static CompiledScene Empty { get; } = new()
    {
        Triangles = ImmutableArray<GpuTriangle>.Empty,
        Materials = ImmutableArray<GpuMaterial>.Empty,
        Lights = ImmutableArray<GpuLight>.Empty,
        BvhNodes = ImmutableArray<BvhNode>.Empty,
        TriangleOrder = ImmutableArray<int>.Empty,
    };
}
