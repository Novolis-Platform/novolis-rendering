using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Persistent ground or wall mark. <see cref="Kind"/> is a game tag.</summary>
public struct SpriteDecal
{
    /// <summary>Gameplay XZ.</summary>
    public Vector3 Position;

    /// <summary>Height along <see cref="PlanarElevation"/>.</summary>
    public float Elevation;

    /// <summary>World width (X).</summary>
    public float Width;

    /// <summary>World height (Z).</summary>
    public float Height;

    /// <summary>Y rotation in radians.</summary>
    public float Rotation;

    /// <summary>Tint.</summary>
    public Rgba32 Color;

    /// <summary>Game-defined mark id.</summary>
    public int Kind;
}
