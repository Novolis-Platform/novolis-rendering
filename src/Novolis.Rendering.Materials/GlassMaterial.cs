using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Transmission and refraction model.</summary>
public sealed record GlassMaterial : IMaterial
{
    public Vector3 Tint { get; init; } = Vector3.One;
    public float Roughness { get; init; }
    public float Ior { get; init; } = 1.5f;
    public float Absorption { get; init; }
}
