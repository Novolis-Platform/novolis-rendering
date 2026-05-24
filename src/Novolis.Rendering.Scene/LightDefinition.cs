using System.Numerics;

namespace Novolis.Rendering.Scene;

/// <summary>Authoring-time light classification.</summary>
public enum LightKind
{
    /// <summary>Directional light; direction stored in <see cref="LightDefinition.DirectionOrPosition"/>.</summary>
    Directional,

    /// <summary>Point light; position stored in <see cref="LightDefinition.DirectionOrPosition"/>.</summary>
    Point,
}

/// <summary>Authoring light definition.</summary>
/// <param name="Kind">Light type.</param>
/// <param name="DirectionOrPosition">Direction or position in world space.</param>
/// <param name="Color">Linear RGB color.</param>
/// <param name="Intensity">Scalar intensity multiplier.</param>
public sealed record LightDefinition(
    LightKind Kind,
    Vector3 DirectionOrPosition,
    Vector3 Color,
    float Intensity);
