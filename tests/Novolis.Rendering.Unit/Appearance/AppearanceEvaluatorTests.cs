using Novolis.Math.Geometry;
using Novolis.Rendering.Appearance;

namespace Novolis.Rendering.Appearance.Tests;

public sealed class AppearanceEvaluatorTests
{
    [Test]
    public async Task Grey_surface_is_boring()
    {
        var sample = AppearanceEvaluator.EvaluateSurface(AppearanceStack.GreySurface(), default);
        var grey = sample.Albedo;
        await Assert.That(grey.R).IsEqualTo(grey.G);
        await Assert.That((int)grey.R).IsGreaterThan(140);
        await Assert.That((int)grey.R).IsLessThan(180);
        await Assert.That(sample.Emission).IsEqualTo(Rgba32.Black);
    }

    [Test]
    public async Task Steel_recipe_is_darker_than_grey()
    {
        var ctx = Uv(0.31f, 0.44f, 11);
        var grey = AppearanceEvaluator.PreviewColor(AppearanceEvaluator.EvaluateSurface(AppearanceStack.GreySurface(), ctx));
        var steel = AppearanceEvaluator.PreviewColor(AppearanceEvaluator.EvaluateSurface(AppearanceRecipes.SteelPanels(), ctx));
        await Assert.That(steel.R + steel.G + steel.B).IsLessThan(grey.R + grey.G + grey.B);
    }

    [Test]
    public async Task Emission_raises_preview_luma()
    {
        var ctx = Uv(0.2f, 0.2f, 3);
        var baseLuma = Luma(AppearanceEvaluator.PreviewColor(AppearanceEvaluator.EvaluateSurface(AppearanceRecipes.SteelPanels(), ctx)));
        var glowLuma = Luma(AppearanceEvaluator.PreviewColor(AppearanceEvaluator.EvaluateSurface(AppearanceRecipes.FilthyGlowWall(), ctx)));
        await Assert.That(glowLuma).IsGreaterThan(baseLuma);
    }

    [Test]
    public async Task Noise_is_deterministic_for_a_seed()
    {
        var stack = AppearanceStack.GreySurface().Then(AppearanceOp.Noise, 0.4f, 3f, seed: 44);
        var ctx = Uv(0.17f, 0.61f, 2);
        var a = AppearanceEvaluator.EvaluateSurface(stack, ctx).Albedo;
        var b = AppearanceEvaluator.EvaluateSurface(stack, ctx).Albedo;
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task Bake_surface_is_not_uniform_grey()
    {
        var pixels = AppearanceBaker.BakeSurface(AppearanceRecipes.SteelPanels(8), 32, 32, 2f, 8);
        var min = 255;
        var max = 0;
        foreach (var p in pixels)
        {
            min = System.Math.Min(min, p.R);
            max = System.Math.Max(max, p.R);
        }

        await Assert.That(max - min).IsGreaterThan(8);
        await Assert.That(pixels.Length).IsEqualTo(32 * 32);
    }

    [Test]
    public async Task Cone_through_fog_is_brighter_on_axis()
    {
        var pixels = AppearanceBaker.BakeLightVolume(
            AppearanceRecipes.Flashlight(),
            AppearanceRecipes.IndoorFog(),
            48,
            48,
            seed: 1);
        var axis = pixels[40 * 48 + 24];
        var edge = pixels[40 * 48 + 2];
        await Assert.That((int)axis.A).IsGreaterThan(edge.A);
    }

    [Test]
    public async Task Post_bloom_lifts_a_hot_pixel_neighborhood()
    {
        var pixels = new Rgba32[9];
        pixels[4] = new Rgba32(255, 255, 255);
        AppearancePost.Apply(AppearanceRecipes.RadioactiveBloom(), pixels, 3, 3);
        await Assert.That((int)pixels[1].R).IsGreaterThan(0);
    }

    [Test]
    public async Task Laser_effect_is_hotter_in_the_core()
    {
        var stack = AppearanceRecipes.LaserBolt();
        var core = AppearanceEvaluator.EvaluateEffect(stack, new AppearanceContext { U = 0.5f, V = 0.5f });
        var rim = AppearanceEvaluator.EvaluateEffect(stack, new AppearanceContext { U = 0.02f, V = 0.5f });
        await Assert.That(core.R + core.G + core.B).IsGreaterThan(rim.R + rim.G + rim.B);
    }

    private static AppearanceContext Uv(float u, float v, int seed) => new()
    {
        U = u,
        V = v,
        Seed = seed,
        Normal = System.Numerics.Vector3.UnitY,
        View = System.Numerics.Vector3.UnitY,
    };

    private static int Luma(Rgba32 c) => c.R + c.G + c.B;
}
