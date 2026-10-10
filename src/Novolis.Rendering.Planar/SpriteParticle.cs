using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Cosmetic planar particle. <see cref="Kind"/> is a game tag.</summary>
public struct SpriteParticle
{
    /// <summary>Gameplay XZ.</summary>
    public Vector3 Position;

    /// <summary>Gameplay XZ velocity.</summary>
    public Vector3 Velocity;

    /// <summary>Height along <see cref="PlanarElevation"/>.</summary>
    public float Elevation;

    /// <summary>Vertical speed for bounce.</summary>
    public float ElevationVelocity;

    /// <summary>Remaining life in seconds.</summary>
    public float Life;

    /// <summary>Initial life (for color/size lerp).</summary>
    public float MaxLife;

    /// <summary>Color at birth.</summary>
    public Rgba32 ColorStart;

    /// <summary>Color at death.</summary>
    public Rgba32 ColorEnd;

    /// <summary>World size at birth.</summary>
    public float SizeStart;

    /// <summary>World size at death.</summary>
    public float SizeEnd;

    /// <summary>Game-defined sprite/recipe id.</summary>
    public int Kind;

    /// <summary>Exponential drag on XZ velocity.</summary>
    public float Drag;

    /// <summary>Radians per second around Y.</summary>
    public float Spin;

    /// <summary>Current Y rotation.</summary>
    public float Rotation;

    /// <summary>When true, skip integrate (stuck decal-like spark).</summary>
    public bool Sticky;
}
