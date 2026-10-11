namespace Novolis.Rendering.FrameGraph;

/// <summary>Stable resource row in a diagnostic graph capture.</summary>
public sealed record FrameGraphDiagnosticResource(
    int Id,
    string Kind,
    string Name,
    string Format,
    int Width,
    int Height,
    int FirstUseIndex,
    int LastUseIndex,
    string Lifetime);
