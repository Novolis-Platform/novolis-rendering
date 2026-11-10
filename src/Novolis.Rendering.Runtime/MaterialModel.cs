using System.Numerics;

namespace Novolis.Rendering.Runtime;

/// <summary>Shading model id packed into <see cref="GpuMaterial"/>.</summary>
public enum MaterialModel
{
    /// <summary>Physically based standard BRDF.</summary>
    Standard,

    /// <summary>Dielectric transmission and refraction.</summary>
    Glass,

    /// <summary>Approximate subsurface scattering.</summary>
    Skin,

    /// <summary>Emissive (light-emitting) surface.</summary>
    Emissive,

    /// <summary>XZ-plane checker albedo (procedural; one ground quad instead of tile boxes).</summary>
    Checker,
}
