using System.Numerics;

namespace Novolis.Rendering.Scene;

public enum LightKind
{
    Directional,
    Point,
}

/// <summary>Authoring light definition.</summary>
public sealed record LightDefinition(
    LightKind Kind,
    Vector3 DirectionOrPosition,
    Vector3 Color,
    float Intensity);
