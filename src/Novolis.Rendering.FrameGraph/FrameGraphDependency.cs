namespace Novolis.Rendering.FrameGraph;

/// <summary>Ordering edge emitted by dependency analysis.</summary>
public readonly record struct FrameGraphDependency(
    FrameGraphPassHandle From,
    FrameGraphPassHandle To,
    FrameGraphDependencyReason Reason,
    FrameGraphResourceHandle? Resource = null);
