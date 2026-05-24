using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Baseline physically-based material (matte, plastic, painted, most metals).</summary>
public sealed record StandardMaterial : IMaterial
{
    public Vector3 BaseColor { get; init; } = Vector3.One;
    public float Roughness { get; init; } = 0.5f;
    public float Metallic { get; init; }
    public float EmissionStrength { get; init; }
    public Vector3 EmissionColor { get; init; } = Vector3.Zero;
}
