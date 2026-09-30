using System.Numerics;

namespace Novolis.Rendering.Runtime;

/// <summary>Runtime light for shading.</summary>
/// <param name="Kind">Light type.</param>
/// <param name="DirectionOrPosition">Direction (directional) or position (point).</param>
/// <param name="Color">Linear RGB color.</param>
/// <param name="Intensity">Scalar intensity multiplier.</param>
public readonly record struct GpuLight(
    GpuLightKind Kind,
    Vector3 DirectionOrPosition,
    Vector3 Color,
    float Intensity);
