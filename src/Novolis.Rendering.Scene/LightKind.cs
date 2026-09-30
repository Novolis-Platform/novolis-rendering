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
