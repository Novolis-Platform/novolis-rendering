namespace Novolis.Rendering.Planar;

/// <summary>One selectable menu row.</summary>
/// <param name="Label">Display label.</param>
/// <param name="Tag">Optional tag returned from <see cref="OnSelect"/>.</param>
/// <param name="OnSelect">Action when confirmed.</param>
public sealed record class PlanarMenuItem(string Label, object? Tag = null, Func<object?>? OnSelect = null);
