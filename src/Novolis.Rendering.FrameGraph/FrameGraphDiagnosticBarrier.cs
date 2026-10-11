namespace Novolis.Rendering.FrameGraph;

/// <summary>Stable barrier row in a diagnostic graph capture.</summary>
public sealed record FrameGraphDiagnosticBarrier(
    int ExecutionIndex,
    int PassId,
    int ResourceId,
    string Before,
    string After,
    string Access,
    string Usage);
