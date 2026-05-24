using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Geometry-driven light emission.</summary>
public sealed record EmissiveMaterial : IMaterial
{
    public Vector3 EmissionColor { get; init; } = Vector3.One;
    public float EmissionStrength { get; init; } = 1f;
}
