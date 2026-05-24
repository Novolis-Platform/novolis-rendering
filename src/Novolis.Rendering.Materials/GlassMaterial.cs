using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Transmission and refraction model.</summary>
public sealed record GlassMaterial : IMaterial
{
    /// <summary>Transmission tint in linear RGB.</summary>
    public Vector3 Tint { get; init; } = Vector3.One;

    /// <summary>Surface roughness for microfacet transmission.</summary>
    public float Roughness { get; init; }

    /// <summary>Index of refraction.</summary>
    public float Ior { get; init; } = 1.5f;

    /// <summary>Beer-Lambert absorption strength.</summary>
    public float Absorption { get; init; }
}
