namespace Novolis.Rendering.FrameGraph;

/// <summary>Static capture entry point for diagnostics tooling and tests.</summary>
public static class FrameGraphCapture
{
    /// <summary>Captures a compiled graph without invoking a target.</summary>
    public static FrameGraphDiagnosticGraph Capture(CompiledFrameGraph graph) =>
        FrameGraphDiagnosticGraph.Capture(graph);
}
