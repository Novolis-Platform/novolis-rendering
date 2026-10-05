using Novolis.Math.Geometry;

namespace Novolis.Rendering.Appearance;

/// <summary>Reusable stacks. Hosts pick recipes; they do not author bitmaps.</summary>
public static class AppearanceRecipes
{
    /// <summary>Dark steel with panel lines, noise, and edge wear.</summary>
    public static AppearanceStack SteelPanels(int seed = 11) =>
        AppearanceStack.GreySurface()
            .Then(AppearanceOp.BaseColor, 1f, color: new Rgba32(78, 86, 96))
            .Then(AppearanceOp.Metallic, 0.82f)
            .Then(AppearanceOp.Roughness, 0.38f)
            .Then(AppearanceOp.Noise, 0.14f, 4.2f, seed: seed)
            .Then(AppearanceOp.PanelGrid, 0.85f, 0.55f, aux: 0.05f)
            .Then(AppearanceOp.EdgeWear, 0.22f, 9f, seed: seed + 3);

    /// <summary>Poured concrete with low-frequency grime.</summary>
    public static AppearanceStack ConcreteGrime(int seed = 21) =>
        AppearanceStack.GreySurface()
            .Then(AppearanceOp.BaseColor, 1f, color: new Rgba32(92, 90, 86))
            .Then(AppearanceOp.Roughness, 0.72f)
            .Then(AppearanceOp.Noise, 0.18f, 2.4f, seed: seed)
            .Then(AppearanceOp.Noise, 0.08f, 11f, seed: seed + 7)
            .Then(AppearanceOp.PanelGrid, 0.35f, 2.2f, aux: 0.02f);

    /// <summary>Tinted glass.</summary>
    public static AppearanceStack GlassPane() =>
        AppearanceStack.GreySurface()
            .Then(AppearanceOp.BaseColor, 1f, color: new Rgba32(140, 168, 180))
            .Then(AppearanceOp.Roughness, 0.08f)
            .Then(AppearanceOp.Transparency, 0.42f)
            .Then(AppearanceOp.Fresnel, 3.2f, color: new Rgba32(220, 236, 255));

    /// <summary>Range floor: stained concrete + lane-scale grid.</summary>
    public static AppearanceStack RangeFloor(int seed = 4) =>
        ConcreteGrime(seed)
            .Then(AppearanceOp.PanelGrid, 0.55f, 5f, aux: 0.018f)
            .Then(AppearanceOp.Mix, 0.12f, color: new Rgba32(40, 42, 46));

    /// <summary>Steel + grime + green emissive cracks (the filthy-glow example).</summary>
    public static AppearanceStack FilthyGlowWall(int seed = 69) =>
        SteelPanels(seed)
            .Then(AppearanceOp.Noise, 0.22f, 0.9f, seed: seed + 1)
            .Then(AppearanceOp.Emission, 1.15f, 3.4f, new Rgba32(40, 255, 90), seed + 9);

    /// <summary>Indoor scattering fog.</summary>
    public static AppearanceStack IndoorFog(int seed = 2) =>
        AppearanceStack.EmptyVolume()
            .Then(AppearanceOp.Density, 0.55f)
            .Then(AppearanceOp.Scattering, 0.55f, color: new Rgba32(210, 214, 220))
            .Then(AppearanceOp.DensityNoise, 1f, 2.8f, seed: seed);

    /// <summary>Handheld cone. Aux is cosine of half-angle (~25°).</summary>
    public static AppearanceStack Flashlight() =>
        AppearanceStack.WhiteLight()
            .Then(AppearanceOp.Cone, 18f, aux: 0.9f)
            .Then(AppearanceOp.Tint, color: new Rgba32(255, 236, 200))
            .Then(AppearanceOp.Falloff, 0.045f)
            .Then(AppearanceOp.Flicker, scale: 9f, seed: 5);

    /// <summary>Orange tracer / laser bolt look.</summary>
    public static AppearanceStack LaserBolt() =>
        AppearanceStack.EmptyEffect()
            .Then(AppearanceOp.Beam, 1.6f, color: new Rgba32(255, 170, 40))
            .Then(AppearanceOp.Emission, 0.45f, color: new Rgba32(255, 220, 120));

    /// <summary>Blue-white plasma core.</summary>
    public static AppearanceStack PlasmaBolt() =>
        AppearanceStack.EmptyEffect()
            .Then(AppearanceOp.Projectile, 1.8f, color: new Rgba32(90, 180, 255))
            .Then(AppearanceOp.Emission, 0.7f, color: new Rgba32(220, 240, 255))
            .Then(AppearanceOp.SparkBurst, 0.55f, 18f, new Rgba32(180, 220, 255), 8);

    /// <summary>Finished-image punch for capture dumps.</summary>
    public static AppearanceStack RadioactiveBloom() =>
        AppearanceStack.PassthroughPost()
            .Then(AppearanceOp.Bloom, 0.45f, 2f, aux: 0.55f)
            .Then(AppearanceOp.Exposure, 1.08f)
            .Then(AppearanceOp.Vignette, 0.35f)
            .Then(AppearanceOp.ToneMap, 0.25f);
}
