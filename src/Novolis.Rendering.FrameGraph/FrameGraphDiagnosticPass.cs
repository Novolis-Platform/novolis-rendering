namespace Novolis.Rendering.FrameGraph;

/// <summary>Stable pass row in a diagnostic graph capture.</summary>
public sealed record FrameGraphDiagnosticPass(
    int Id,
    int ExecutionIndex,
    string Name,
    string Kind,
    IReadOnlyList<int> Dependencies,
    IReadOnlyList<int> Resources,
    string RequiredFeatures)
{
    /// <summary>Typed renderer pipeline stage.</summary>
    public string Stage { get; init; } = string.Empty;
}
