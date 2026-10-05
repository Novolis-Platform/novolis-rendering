using Novolis.Math.Geometry;

namespace Novolis.Rendering.Appearance;

/// <summary>Illumination after evaluating a light stack at a sample.</summary>
public readonly record struct LightSample(Rgba32 Color, float Intensity);
