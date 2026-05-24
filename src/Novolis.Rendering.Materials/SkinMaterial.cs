using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Approximate subsurface scattering model.</summary>
public sealed record SkinMaterial : IMaterial
{
    /// <summary>Base skin tone in linear RGB.</summary>
    public Vector3 BaseColor { get; init; } = new(0.85f, 0.65f, 0.55f);

    /// <summary>Surface roughness.</summary>
    public float Roughness { get; init; } = 0.45f;

    /// <summary>Subsurface scattering strength.</summary>
    public float SubsurfaceStrength { get; init; } = 0.5f;

    /// <summary>Blood layer tint for scattering.</summary>
    public Vector3 BloodTint { get; init; } = new(0.8f, 0.15f, 0.08f);

    /// <summary>Scatter radius in world units.</summary>
    public float ScatterRadius { get; init; } = 0.02f;
}
