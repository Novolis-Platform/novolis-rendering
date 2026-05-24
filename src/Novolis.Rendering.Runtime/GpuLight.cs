using System.Numerics;

namespace Novolis.Rendering.Runtime;

public enum GpuLightKind : byte
{
    Directional,
    Point,
}

/// <summary>Runtime light for shading.</summary>
public readonly record struct GpuLight(
    GpuLightKind Kind,
    Vector3 DirectionOrPosition,
    Vector3 Color,
    float Intensity);
