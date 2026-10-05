using Novolis.Math.Geometry;

namespace Novolis.Rendering.Appearance;

/// <summary>One parameterized operation. This record <em>is</em> the asset — not a bitmap.</summary>
public readonly record struct AppearanceLayer(
    AppearanceOp Op,
    float Amount = 1f,
    float Scale = 1f,
    Rgba32 Color = default,
    int Seed = 0,
    float Aux = 0f);
