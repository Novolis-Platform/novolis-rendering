namespace Novolis.Rendering.FrameGraph;

/// <summary>Immutable result from validating a graph description.</summary>
public sealed class FrameGraphValidationResult
{
    internal FrameGraphValidationResult(IReadOnlyList<FrameGraphDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
    }

    /// <summary>All diagnostics in deterministic discovery order.</summary>
    public IReadOnlyList<FrameGraphDiagnostic> Diagnostics { get; }

    /// <summary>Whether no error diagnostics were found.</summary>
    public bool IsValid => Diagnostics.All(diagnostic => diagnostic.Severity != FrameGraphDiagnosticSeverity.Error);

    /// <summary>Whether at least one warning was found.</summary>
    public bool HasWarnings => Diagnostics.Any(diagnostic => diagnostic.Severity == FrameGraphDiagnosticSeverity.Warning);

    /// <summary>Returns only error diagnostics.</summary>
    public IReadOnlyList<FrameGraphDiagnostic> Errors =>
        Diagnostics.Where(diagnostic => diagnostic.Severity == FrameGraphDiagnosticSeverity.Error).ToArray();

    /// <summary>Throws when the result contains errors.</summary>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new FrameGraphValidationException(Diagnostics);
        }
    }
}
