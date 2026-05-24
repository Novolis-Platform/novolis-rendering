using System.Collections.Immutable;

namespace Novolis.Rendering.Runtime;

/// <summary>Flat runtime scene consumed by ray tracing backends.</summary>
public sealed record CompiledScene
{
    public required ImmutableArray<GpuTriangle> Triangles { get; init; }
    public required ImmutableArray<GpuMaterial> Materials { get; init; }
    public required ImmutableArray<GpuLight> Lights { get; init; }
    public required ImmutableArray<BvhNode> BvhNodes { get; init; }
    public required ImmutableArray<int> TriangleOrder { get; init; }
    public int BvhRootIndex { get; init; } = -1;

    public static CompiledScene Empty { get; } = new()
    {
        Triangles = ImmutableArray<GpuTriangle>.Empty,
        Materials = ImmutableArray<GpuMaterial>.Empty,
        Lights = ImmutableArray<GpuLight>.Empty,
        BvhNodes = ImmutableArray<BvhNode>.Empty,
        TriangleOrder = ImmutableArray<int>.Empty,
    };
}
