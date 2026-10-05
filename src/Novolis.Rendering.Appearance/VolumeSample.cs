using Novolis.Math.Geometry;

namespace Novolis.Rendering.Appearance;

/// <summary>Volume properties after evaluating a volume stack.</summary>
public readonly record struct VolumeSample(
    float Density,
    Rgba32 Scatter,
    Rgba32 Absorb);
