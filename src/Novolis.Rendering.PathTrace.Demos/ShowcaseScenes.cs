using System.Numerics;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Scene;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>Authoring scenes shared by Raylib and Silk path-tracing dogfood apps.</summary>
public static class ShowcaseScenes
{
    /// <summary>Focus point for orbit cameras in showcase scenes.</summary>
    public static readonly Vector3 OrbitTarget = new(0f, 0.25f, 0f);

    /// <summary>Default static eye for the hello showcase.</summary>
    public static readonly Vector3 HelloDefaultEye = new(1.2f, 0.8f, 2f);

    /// <summary>Builds the standard hello scene (ground, red cube, silver slab, two lights).</summary>
    /// <returns>A compiled scene.</returns>
    public static CompiledScene BuildHelloShowcase()
    {
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(new Vector3(0.42f, 0.44f, 0.48f), roughness: 0.92f))
            .AddBox(
                OrbitTarget,
                new Vector3(0.25f, 0.25f, 0.25f),
                MaterialPresets.Standard(MaterialPresets.Colors.Red, roughness: 0.4f, metallic: 0.05f))
            .AddBox(
                new Vector3(-0.45f, 0.32f, 0.12f),
                new Vector3(0.3f, 0.32f, 0.025f),
                MaterialPresets.Metal(MaterialPresets.Colors.Silver, roughness: 0.06f))
            .AddDirectionalLight(new Vector3(-0.35f, -1f, -0.25f), new Vector3(1f, 0.98f, 0.95f), 1.1f)
            .AddDirectionalLight(new Vector3(0.6f, -0.4f, 0.5f), new Vector3(0.55f, 0.65f, 0.85f), 0.45f)
            .Build();
        return SceneCompiler.Compile(scene);
    }

    /// <summary>Builds a richer studio scene with glass and emissive accents.</summary>
    /// <returns>A compiled scene.</returns>
    public static CompiledScene BuildStudioShowcase()
    {
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(new Vector3(0.38f, 0.4f, 0.44f), roughness: 0.88f))
            .AddBox(
                OrbitTarget,
                new Vector3(0.25f, 0.25f, 0.25f),
                MaterialPresets.Standard(MaterialPresets.Colors.Red, roughness: 0.35f, metallic: 0.08f))
            .AddBox(
                new Vector3(-0.5f, 0.34f, 0.1f),
                new Vector3(0.28f, 0.34f, 0.03f),
                MaterialPresets.Metal(MaterialPresets.Colors.Silver, roughness: 0.05f))
            .AddBox(
                new Vector3(0.42f, 0.18f, -0.35f),
                new Vector3(0.12f, 0.12f, 0.12f),
                MaterialPresets.Glass(tint: new Vector3(0.75f, 0.9f, 1f), roughness: 0.02f, ior: 1.45f))
            .AddBox(
                new Vector3(-0.12f, 0.55f, 0.38f),
                new Vector3(0.04f, 0.04f, 0.04f),
                MaterialPresets.Emissive(new Vector3(1f, 0.55f, 0.2f), strength: 4f))
            .AddDirectionalLight(new Vector3(-0.35f, -1f, -0.25f), new Vector3(1f, 0.98f, 0.95f), 1.15f)
            .AddDirectionalLight(new Vector3(0.55f, -0.35f, 0.45f), new Vector3(0.5f, 0.62f, 0.9f), 0.5f)
            .AddDirectionalLight(new Vector3(0.2f, -0.15f, -0.9f), new Vector3(0.25f, 0.3f, 0.4f), 0.25f)
            .Build();
        return SceneCompiler.Compile(scene);
    }
}
