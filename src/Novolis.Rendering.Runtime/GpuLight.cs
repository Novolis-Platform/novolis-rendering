using System.Numerics;

namespace Novolis.Rendering.Runtime;

/// <summary>Runtime light for shading.</summary>
/// <param name="Kind">Light type.</param>
/// <param name="DirectionOrPosition">Direction (directional), position (point), or rect center.</param>
/// <param name="Color">Linear RGB color.</param>
/// <param name="Intensity">Scalar intensity / radiance multiplier.</param>
/// <param name="U">Rect half-extent axis (zero for point/directional).</param>
/// <param name="V">Rect half-extent axis (zero for point/directional).</param>
public readonly record struct GpuLight(
    GpuLightKind Kind,
    Vector3 DirectionOrPosition,
    Vector3 Color,
    float Intensity,
    Vector3 U = default,
    Vector3 V = default);
