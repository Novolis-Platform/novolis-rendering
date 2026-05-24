using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Approximate subsurface scattering model.</summary>
public sealed record SkinMaterial : IMaterial
{
    public Vector3 BaseColor { get; init; } = new(0.85f, 0.65f, 0.55f);
    public float Roughness { get; init; } = 0.45f;
    public float SubsurfaceStrength { get; init; } = 0.5f;
    public Vector3 BloodTint { get; init; } = new(0.8f, 0.15f, 0.08f);
    public float ScatterRadius { get; init; } = 0.02f;
}
