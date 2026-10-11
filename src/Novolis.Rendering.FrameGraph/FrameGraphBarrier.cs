namespace Novolis.Rendering.FrameGraph;

/// <summary>State transition required before a pass access.</summary>
public readonly record struct FrameGraphBarrier(
    int ExecutionIndex,
    FrameGraphPassHandle Pass,
    FrameGraphResourceHandle Resource,
    FrameGraphResourceState Before,
    FrameGraphResourceState After,
    FrameGraphResourceAccessKind Access,
    FrameGraphResourceUsage Usage);
