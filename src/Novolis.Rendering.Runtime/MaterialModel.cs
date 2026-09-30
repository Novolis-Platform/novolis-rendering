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
}
