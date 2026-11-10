using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Procedural checkerboard albedo in world XZ (cheap floor for path-trace demos).</summary>
public sealed record CheckerMaterial : IMaterial
{
    /// <summary>Even-tile color.</summary>
    public Vector3 ColorA { get; init; } = new(0.82f, 0.8f, 0.74f);

    /// <summary>Odd-tile color.</summary>
    public Vector3 ColorB { get; init; } = new(0.16f, 0.16f, 0.18f);

    /// <summary>Tiles per world meter.</summary>
    public float Scale { get; init; } = 2.5f;

    /// <summary>Surface roughness.</summary>
    public float Roughness { get; init; } = 0.9f;
}
