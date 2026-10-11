namespace Novolis.Rendering.FrameGraph;

/// <summary>Result returned by a non-throwing compilation attempt.</summary>
public sealed class FrameGraphCompilationResult
{
    internal FrameGraphCompilationResult(
        CompiledFrameGraph? plan,
        IReadOnlyList<FrameGraphDiagnostic> diagnostics)
    {
        Plan = plan;
        Diagnostics = diagnostics;
    }

    /// <summary>Compiled plan when <see cref="Succeeded"/> is true.</summary>
    public CompiledFrameGraph? Plan { get; }

    /// <summary>Validation or cycle diagnostics.</summary>
    public IReadOnlyList<FrameGraphDiagnostic> Diagnostics { get; }

    /// <summary>Whether compilation produced a plan.</summary>
    public bool Succeeded => Plan is not null && Diagnostics.All(diagnostic => diagnostic.Severity != FrameGraphDiagnosticSeverity.Error);
}
