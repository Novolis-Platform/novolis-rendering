namespace Novolis.Rendering.Planar;

/// <summary>Shape of a host-neutral planar light source.</summary>
public enum PlanarLightKind
{
    /// <summary>Omnidirectional local light.</summary>
    Point,

    /// <summary>Directional cone light.</summary>
    Spot,

    /// <summary>Light-emitting surface or unattenuated visual source.</summary>
    Emissive,
}
