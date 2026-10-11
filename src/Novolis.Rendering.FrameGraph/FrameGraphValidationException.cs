namespace Novolis.Rendering.FrameGraph;

/// <summary>Exception raised when a graph cannot be compiled safely.</summary>
public sealed class FrameGraphValidationException : InvalidOperationException
{
    /// <summary>Creates an exception containing stable graph diagnostics.</summary>
    public FrameGraphValidationException(IReadOnlyList<FrameGraphDiagnostic> diagnostics)
        : base(CreateMessage(diagnostics))
    {
        Diagnostics = diagnostics;
    }

    /// <summary>Diagnostics that prevented compilation.</summary>
    public IReadOnlyList<FrameGraphDiagnostic> Diagnostics { get; }

    private static string CreateMessage(IReadOnlyList<FrameGraphDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var errors = diagnostics
            .Where(diagnostic => diagnostic.Severity == FrameGraphDiagnosticSeverity.Error)
            .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}");
        return $"Frame graph validation failed.{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
    }
}
