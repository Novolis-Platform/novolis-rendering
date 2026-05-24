using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Baseline physically-based material (matte, plastic, painted, most metals).</summary>
public sealed record StandardMaterial : IMaterial
{
    /// <summary>Diffuse albedo in linear RGB.</summary>
    public Vector3 BaseColor { get; init; } = Vector3.One;

    /// <summary>Surface roughness in [0, 1].</summary>
    public float Roughness { get; init; } = 0.5f;

    /// <summary>Metallic factor in [0, 1].</summary>
    public float Metallic { get; init; }

    /// <summary>Emission strength multiplier.</summary>
    public float EmissionStrength { get; init; }

    /// <summary>Emission color in linear RGB.</summary>
    public Vector3 EmissionColor { get; init; } = Vector3.Zero;
}
