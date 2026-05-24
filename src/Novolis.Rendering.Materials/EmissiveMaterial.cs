using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Geometry-driven light emission.</summary>
public sealed record EmissiveMaterial : IMaterial
{
    /// <summary>Emission color in linear RGB.</summary>
    public Vector3 EmissionColor { get; init; } = Vector3.One;

    /// <summary>Emission strength multiplier.</summary>
    public float EmissionStrength { get; init; } = 1f;
}
