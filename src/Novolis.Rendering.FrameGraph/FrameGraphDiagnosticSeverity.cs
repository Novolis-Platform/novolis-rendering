namespace Novolis.Rendering.FrameGraph;

/// <summary>Severity assigned to graph validation and target diagnostics.</summary>
public enum FrameGraphDiagnosticSeverity
{
    /// <summary>Informational diagnostic.</summary>
    Info,
    /// <summary>Non-fatal diagnostic.</summary>
    Warning,
    /// <summary>Compilation-blocking diagnostic.</summary>
    Error,
}
