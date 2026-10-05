using Novolis.Math.Geometry;

namespace Novolis.Rendering.Appearance;

/// <summary>Surface properties after evaluating a surface stack.</summary>
public readonly record struct SurfaceSample(
    Rgba32 Albedo,
    float Roughness,
    float Metallic,
    Rgba32 Emission,
    float Alpha);
