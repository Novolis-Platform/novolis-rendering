namespace Novolis.Rendering.FrameGraph;

/// <summary>One stable validation or target capability diagnostic.</summary>
public sealed record FrameGraphDiagnostic(
    FrameGraphDiagnosticSeverity Severity,
    string Code,
    string Message,
    FrameGraphPassHandle? Pass = null,
    FrameGraphResourceHandle? Resource = null);
