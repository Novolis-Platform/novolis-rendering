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
    public static readonly Vector3 OrbitTarget = new(0f, 0.42f, 0f);

    /// <summary>Default static eye for the hello showcase.</summary>
    public static readonly Vector3 HelloDefaultEye = new(1.2f, 0.8f, 2f);

    /// <summary>Default static eye looking into the studio diorama open face.</summary>
    public static readonly Vector3 StudioDefaultEye = new(0.35f, 0.72f, 2.35f);

    /// <summary>Builds the standard hello scene (ground, red cube, silver slab, two lights).</summary>
    /// <returns>A compiled scene.</returns>
    public static CompiledScene BuildHelloShowcase()
    {
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(new Vector3(0.42f, 0.44f, 0.48f), roughness: 0.92f))
            .AddBox(
                new Vector3(0f, 0.25f, 0f),
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

    /// <summary>
    /// Builds an open-face diorama box (colored walls, checkered floor) packed with
    /// dielectrics, metals, glass, skin, and emissive so bounce light is visible.
    /// </summary>
    /// <returns>A compiled scene.</returns>
    public static CompiledScene BuildStudioShowcase()
    {
        const float halfW = 0.95f;
        const float halfD = 0.95f;
        const float height = 1.15f;
        const float wallT = 0.03f;

        var white = MaterialPresets.Standard(new Vector3(0.86f, 0.86f, 0.84f), roughness: 0.78f);
        var redWall = MaterialPresets.Standard(new Vector3(0.78f, 0.08f, 0.06f), roughness: 0.72f);
        var greenWall = MaterialPresets.Standard(new Vector3(0.08f, 0.55f, 0.14f), roughness: 0.72f);
        var ceiling = MaterialPresets.Standard(new Vector3(0.9f, 0.9f, 0.88f), roughness: 0.85f);

        var builder = new SceneBuilder();
        AddDioramaShell(builder, halfW, halfD, height, wallT, redWall, greenWall, white, ceiling);
        AddCheckerFloor(builder, halfW, halfD, tiles: 10);
        AddStudioProps(builder);
        AddStudioLights(builder, height);

        return SceneCompiler.Compile(builder.Build());
    }

    private static void AddDioramaShell(
        SceneBuilder builder,
        float halfW,
        float halfD,
        float height,
        float wallT,
        IMaterial redWall,
        IMaterial greenWall,
        IMaterial backWall,
        IMaterial ceiling)
    {
        var midY = height * 0.5f;
        // Open face toward +Z (default orbit eye). Left / right / back / ceiling only.
        builder
            .AddBox(
                new Vector3(-halfW - wallT * 0.5f, midY, 0f),
                new Vector3(wallT * 0.5f, midY, halfD + wallT),
                redWall)
            .AddBox(
                new Vector3(halfW + wallT * 0.5f, midY, 0f),
                new Vector3(wallT * 0.5f, midY, halfD + wallT),
                greenWall)
            .AddBox(
                new Vector3(0f, midY, -halfD - wallT * 0.5f),
                new Vector3(halfW + wallT, midY, wallT * 0.5f),
                backWall)
            .AddBox(
                new Vector3(0f, height + wallT * 0.5f, 0f),
                new Vector3(halfW + wallT, wallT * 0.5f, halfD + wallT),
                ceiling);
    }

    private static void AddCheckerFloor(SceneBuilder builder, float halfW, float halfD, int tiles)
    {
        var light = MaterialPresets.Standard(new Vector3(0.82f, 0.8f, 0.74f), roughness: 0.9f);
        var dark = MaterialPresets.Standard(new Vector3(0.16f, 0.16f, 0.18f), roughness: 0.88f);
        var tileW = (halfW * 2f) / tiles;
        var tileD = (halfD * 2f) / tiles;
        var halfTile = new Vector3(tileW * 0.5f - 0.001f, 0.012f, tileD * 0.5f - 0.001f);

        for (var z = 0; z < tiles; z++)
        {
            for (var x = 0; x < tiles; x++)
            {
                var cx = -halfW + (x + 0.5f) * tileW;
                var cz = -halfD + (z + 0.5f) * tileD;
                var mat = ((x + z) & 1) == 0 ? light : dark;
                builder.AddBox(new Vector3(cx, halfTile.Y, cz), halfTile, mat);
            }
        }
    }

    private static void AddStudioProps(SceneBuilder builder)
    {
        builder
            // Classic matte red block
            .AddBox(
                new Vector3(-0.28f, 0.22f, -0.18f),
                new Vector3(0.22f, 0.22f, 0.22f),
                MaterialPresets.Standard(MaterialPresets.Colors.Red, roughness: 0.55f, metallic: 0.02f))
            // Tall brushed gold
            .AddBox(
                new Vector3(0.38f, 0.36f, -0.32f),
                new Vector3(0.12f, 0.36f, 0.12f),
                MaterialPresets.Metal(new Vector3(0.92f, 0.72f, 0.28f), roughness: 0.18f))
            // Mirror slab
            .AddBox(
                new Vector3(-0.58f, 0.4f, 0.28f),
                new Vector3(0.04f, 0.4f, 0.28f),
                MaterialPresets.Metal(MaterialPresets.Colors.Silver, roughness: 0.03f))
            // Clear glass cube
            .AddBox(
                new Vector3(0.18f, 0.16f, 0.32f),
                new Vector3(0.14f, 0.14f, 0.14f),
                MaterialPresets.Glass(tint: new Vector3(0.92f, 0.96f, 1f), roughness: 0.01f, ior: 1.5f, absorption: 0.15f))
            // Tinted glass brick
            .AddBox(
                new Vector3(0.52f, 0.12f, 0.18f),
                new Vector3(0.1f, 0.1f, 0.18f),
                MaterialPresets.Glass(tint: new Vector3(0.45f, 0.85f, 0.55f), roughness: 0.04f, ior: 1.45f, absorption: 0.55f))
            // Rough blue plastic
            .AddBox(
                new Vector3(-0.05f, 0.1f, 0.48f),
                new Vector3(0.16f, 0.08f, 0.12f),
                MaterialPresets.Standard(new Vector3(0.15f, 0.28f, 0.78f), roughness: 0.7f))
            // Glossy white ceramic
            .AddBox(
                new Vector3(0.55f, 0.08f, -0.55f),
                new Vector3(0.14f, 0.08f, 0.14f),
                MaterialPresets.Standard(new Vector3(0.95f, 0.95f, 0.93f), roughness: 0.12f))
            // Copper sphere stand-in
            .AddBox(
                new Vector3(-0.55f, 0.12f, -0.45f),
                new Vector3(0.12f, 0.12f, 0.12f),
                MaterialPresets.Metal(new Vector3(0.85f, 0.42f, 0.22f), roughness: 0.22f))
            // Soft flesh dielectric (Skin GPU path still shares Standard scatter)
            .AddBox(
                new Vector3(0.05f, 0.14f, -0.55f),
                new Vector3(0.12f, 0.14f, 0.12f),
                MaterialPresets.Standard(new Vector3(0.78f, 0.52f, 0.42f), roughness: 0.48f))
            // Dark rubber
            .AddBox(
                new Vector3(-0.35f, 0.06f, 0.55f),
                new Vector3(0.18f, 0.05f, 0.1f),
                MaterialPresets.Standard(new Vector3(0.05f, 0.05f, 0.06f), roughness: 0.95f))
            // Neon lightsaber: dark hilt + cyan blade (emissive → point-light NEE at compile)
            .AddBox(
                new Vector3(0.72f, 0.08f, 0.05f),
                new Vector3(0.028f, 0.08f, 0.028f),
                MaterialPresets.Metal(new Vector3(0.12f, 0.12f, 0.14f), roughness: 0.35f))
            .AddBox(
                new Vector3(0.72f, 0.48f, 0.05f),
                new Vector3(0.018f, 0.32f, 0.018f),
                MaterialPresets.Emissive(new Vector3(0.35f, 0.95f, 1f), strength: 22f))
            // Magenta neon stick
            .AddBox(
                new Vector3(-0.72f, 0.42f, 0.15f),
                new Vector3(0.016f, 0.38f, 0.016f),
                MaterialPresets.Emissive(new Vector3(1f, 0.2f, 0.75f), strength: 16f))
            // Small warm emissive accent
            .AddBox(
                new Vector3(0.55f, 0.55f, 0.55f),
                new Vector3(0.035f, 0.035f, 0.035f),
                MaterialPresets.Emissive(new Vector3(1f, 0.55f, 0.2f), strength: 6f));
    }

    private static void AddStudioLights(SceneBuilder builder, float height)
    {
        // Ceiling softbox only — no directional sun. Emissive meshes are also extracted to
        // point lights at compile so DirectLighting can NEE them (kills the muddy lucky-hit GI).
        builder.AddBox(
            new Vector3(0f, height - 0.02f, -0.05f),
            new Vector3(0.32f, 0.015f, 0.24f),
            MaterialPresets.Emissive(new Vector3(1f, 0.96f, 0.9f), strength: 10f));
    }
}
