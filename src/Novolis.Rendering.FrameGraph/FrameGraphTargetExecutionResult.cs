namespace Novolis.Rendering.FrameGraph;

/// <summary>Result from a neutral target execution.</summary>
public sealed class FrameGraphTargetExecutionResult
{
    internal FrameGraphTargetExecutionResult(
        bool succeeded,
        int executedPassCount,
        FrameGraphDiagnosticGraph? diagnosticGraph,
        IReadOnlyList<FrameGraphDiagnostic> diagnostics,
        FrameGraphTargetDescription? targetDescription)
    {
        Succeeded = succeeded;
        ExecutedPassCount = executedPassCount;
        DiagnosticGraph = diagnosticGraph;
        Diagnostics = diagnostics;
        TargetDescription = targetDescription;
    }

    /// <summary>Whether validation and target compatibility checks succeeded.</summary>
    public bool Succeeded { get; }

    /// <summary>Number of passes accepted in execution order.</summary>
    public int ExecutedPassCount { get; }

    /// <summary>Deterministic graph capture when execution was accepted.</summary>
    public FrameGraphDiagnosticGraph? DiagnosticGraph { get; }

    /// <summary>Target diagnostics.</summary>
    public IReadOnlyList<FrameGraphDiagnostic> Diagnostics { get; }

    /// <summary>Target contract used to validate the graph.</summary>
    public FrameGraphTargetDescription? TargetDescription { get; }
}
