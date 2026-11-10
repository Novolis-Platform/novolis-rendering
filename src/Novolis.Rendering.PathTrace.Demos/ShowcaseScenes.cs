using System.Numerics;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Scene;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>
/// Authoring scenes shared by Raylib and Silk path-tracing dogfood apps.
/// Studio diorama is the capability test for <see cref="MaterialPresets"/> /
/// <see cref="MaterialPresets.Showcase"/> (Standard, Metal, Glass, Skin, Emissive/Neon).
/// </summary>
public static class ShowcaseScenes
{
    /// <summary>Focus point for orbit cameras in showcase scenes.</summary>
    public static readonly Vector3 OrbitTarget = new(0f, 0.42f, 0f);

    /// <summary>Default static eye for the hello showcase.</summary>
    public static readonly Vector3 HelloDefaultEye = new(1.2f, 0.8f, 2f);

    /// <summary>Default static eye looking into the studio diorama open face.</summary>
    public static readonly Vector3 StudioDefaultEye = new(0.35f, 0.72f, 2.35f);

    /// <summary>Builds the standard hello scene (ground, red cube, silver slab, two lights).</summary>
    public static CompiledScene BuildHelloShowcase()
    {
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(new Vector3(0.42f, 0.44f, 0.48f), roughness: 0.92f))
            .AddBox(
                new Vector3(0f, 0.25f, 0f),
                new Vector3(0.25f, 0.25f, 0.25f),
                MaterialPresets.Showcase.MatteRed())
            .AddBox(
                new Vector3(-0.45f, 0.32f, 0.12f),
                new Vector3(0.3f, 0.32f, 0.025f),
                MaterialPresets.Showcase.Mirror())
            .AddDirectionalLight(new Vector3(-0.35f, -1f, -0.25f), new Vector3(1f, 0.98f, 0.95f), 1.1f)
            .AddDirectionalLight(new Vector3(0.6f, -0.4f, 0.5f), new Vector3(0.55f, 0.65f, 0.85f), 0.45f)
            .Build();
        return SceneCompiler.Compile(scene);
    }

    /// <summary>
    /// Open-face diorama: colored walls, checkered floor, and one prop per Materials model
    /// (matte / metal / glass / skin / neon emissive) so bounce and NEE stay visible.
    /// </summary>
    public static CompiledScene BuildStudioShowcase()
    {
        const float halfW = 0.95f;
        const float halfD = 0.95f;
        const float height = 1.15f;
        const float wallT = 0.03f;

        var builder = new SceneBuilder();
        AddDioramaShell(builder, halfW, halfD, height, wallT);
        AddCheckerFloor(builder, halfW, halfD, tiles: 6);
        AddStudioProps(builder);
        builder.AddBox(
            new Vector3(0f, height - 0.02f, -0.05f),
            new Vector3(0.32f, 0.015f, 0.24f),
            MaterialPresets.Showcase.Softbox());

        return SceneCompiler.Compile(builder.Build());
    }

    private static void AddDioramaShell(
        SceneBuilder builder,
        float halfW,
        float halfD,
        float height,
        float wallT)
    {
        var midY = height * 0.5f;
        var plaster = MaterialPresets.Showcase.Plaster();
        builder
            .AddBox(
                new Vector3(-halfW - wallT * 0.5f, midY, 0f),
                new Vector3(wallT * 0.5f, midY, halfD + wallT),
                MaterialPresets.Showcase.CornellRedWall())
            .AddBox(
                new Vector3(halfW + wallT * 0.5f, midY, 0f),
                new Vector3(wallT * 0.5f, midY, halfD + wallT),
                MaterialPresets.Showcase.CornellGreenWall())
            .AddBox(
                new Vector3(0f, midY, -halfD - wallT * 0.5f),
                new Vector3(halfW + wallT, midY, wallT * 0.5f),
                plaster)
            .AddBox(
                new Vector3(0f, height + wallT * 0.5f, 0f),
                new Vector3(halfW + wallT, wallT * 0.5f, halfD + wallT),
                plaster);
    }

    private static void AddCheckerFloor(SceneBuilder builder, float halfW, float halfD, int tiles)
    {
        var light = MaterialPresets.Showcase.CheckerLight();
        var dark = MaterialPresets.Showcase.CheckerDark();
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
            .AddBox(
                new Vector3(-0.28f, 0.22f, -0.18f),
                new Vector3(0.22f, 0.22f, 0.22f),
                MaterialPresets.Showcase.MatteRed())
            .AddBox(
                new Vector3(0.38f, 0.36f, -0.32f),
                new Vector3(0.12f, 0.36f, 0.12f),
                MaterialPresets.Showcase.BrushedGold())
            .AddBox(
                new Vector3(-0.58f, 0.4f, 0.28f),
                new Vector3(0.04f, 0.4f, 0.28f),
                MaterialPresets.Showcase.Mirror())
            .AddBox(
                new Vector3(0.18f, 0.16f, 0.32f),
                new Vector3(0.14f, 0.14f, 0.14f),
                MaterialPresets.Showcase.ClearGlass())
            .AddBox(
                new Vector3(0.52f, 0.12f, 0.18f),
                new Vector3(0.1f, 0.1f, 0.18f),
                MaterialPresets.Showcase.GreenGlass())
            .AddBox(
                new Vector3(-0.05f, 0.1f, 0.48f),
                new Vector3(0.16f, 0.08f, 0.12f),
                MaterialPresets.Showcase.BluePlastic())
            .AddBox(
                new Vector3(0.55f, 0.08f, -0.55f),
                new Vector3(0.14f, 0.08f, 0.14f),
                MaterialPresets.Showcase.Ceramic())
            .AddBox(
                new Vector3(-0.55f, 0.12f, -0.45f),
                new Vector3(0.12f, 0.12f, 0.12f),
                MaterialPresets.Showcase.Copper())
            .AddBox(
                new Vector3(0.05f, 0.14f, -0.55f),
                new Vector3(0.12f, 0.14f, 0.12f),
                MaterialPresets.Showcase.Flesh())
            .AddBox(
                new Vector3(-0.35f, 0.06f, 0.55f),
                new Vector3(0.18f, 0.05f, 0.1f),
                MaterialPresets.Showcase.Rubber())
            .AddBox(
                new Vector3(0.72f, 0.08f, 0.05f),
                new Vector3(0.028f, 0.08f, 0.028f),
                MaterialPresets.Showcase.SaberHilt())
            .AddBox(
                new Vector3(0.72f, 0.48f, 0.05f),
                new Vector3(0.018f, 0.32f, 0.018f),
                MaterialPresets.Showcase.LightsaberCyan())
            .AddBox(
                new Vector3(-0.72f, 0.42f, 0.15f),
                new Vector3(0.016f, 0.38f, 0.016f),
                MaterialPresets.Showcase.NeonMagenta())
            .AddBox(
                new Vector3(0.55f, 0.55f, 0.55f),
                new Vector3(0.035f, 0.035f, 0.035f),
                MaterialPresets.Showcase.WarmAccent());
    }
}
