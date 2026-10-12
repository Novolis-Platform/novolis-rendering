using Novolis.Math.Geometry;
using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarSceneTessellatorTests
{
    [Test]
    public async Task Tessellate_emits_platform_triangles_and_hud_glyphs()
    {
        var scene = new PlanarScene();
        scene.Camera.Position = default;
        scene.Camera.WorldUnitsPerPixel = 1f / 32f;
        scene.AddPlatform(-2f, -1f, 2f, 0f, new Rgba32(80, 160, 80));
        scene.Hud.AddText("HI", 8f, 8f, scale: 2f, Rgba32.White);

        var list = scene.Tessellate(320, 180);

        await Assert.That(list.ViewportWidth).IsEqualTo(320);
        await Assert.That(list.ViewportHeight).IsEqualTo(180);
        await Assert.That(list.Triangles.Length > 0).IsTrue();
        await Assert.That(list.Textures.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Tessellate_includes_registered_sprite_texture()
    {
        var scene = new PlanarScene();
        var pixels = new Rgba32[4];
        var id = scene.Textures.Register(pixels, 2, 2, "unit");
        scene.Sprites.Add(new PlanarSpriteInstance
        {
            Texture = id,
            Transform = new PlanarTransform { Position = new System.Numerics.Vector3(0f, 0f, 0f), Scale = new System.Numerics.Vector3(1f, 0f, 1f) },
        });

        var list = scene.Tessellate(64, 64);

        await Assert.That(list.Textures.Length).IsEqualTo(1);
        await Assert.That(list.Textures[0].Id).IsEqualTo(id.Value);
        await Assert.That(list.Triangles.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Tessellate_interleaves_textured_polygons_and_sprites_in_one_queue()
    {
        var scene = new PlanarScene();
        var polygonTexture = scene.Textures.Solid(new Rgba32(20, 80, 120), "polygon");
        var spriteTexture = scene.Textures.Solid(new Rgba32(180, 120, 40), "sprite");
        var polygon = new PlanarStaticPolygon(
            PlanarScenePrimitives.Rectangle(-2f, -2f, 2f, 2f),
            Rgba32.White)
        {
            Texture = polygonTexture,
            SortKey = 10,
        };
        scene.StaticPolygons.Add(polygon);
        scene.Sprites.Add(
            new PlanarSpriteInstance
            {
                Texture = spriteTexture,
                SortKey = 20,
                Transform =
                {
                    Position = new System.Numerics.Vector3(0f, 0f, 0f),
                    Scale = new System.Numerics.Vector3(1f, 1f, 1f),
                },
            });

        var list = scene.Tessellate(64, 64);

        await Assert.That(list.Triangles[0].TextureId).IsEqualTo(polygonTexture.Value);
        await Assert.That(list.Triangles[2].TextureId).IsEqualTo(spriteTexture.Value);
    }

    [Test]
    public async Task Lighting_falls_off_with_distance_and_preserves_alpha()
    {
        var lighting = new PlanarLighting
        {
            AmbientColor = Rgba32.White,
            AmbientIntensity = 0.1f,
        };
        lighting.Sources.Add(
            new PlanarLightSource(
                System.Numerics.Vector3.Zero,
                new Rgba32(255, 180, 80),
                5f,
                1.2f,
                PlanarLightKind.Point,
                System.Numerics.Vector3.Zero,
                MathF.PI));

        var near = lighting.Shade(
            System.Numerics.Vector3.Zero,
            new Rgba32(100, 100, 100, 123));
        var far = lighting.Shade(
            new System.Numerics.Vector3(4.9f, 0f, 0f),
            new Rgba32(100, 100, 100, 123));

        await Assert.That(near.R).IsGreaterThan(far.R);
        await Assert.That(near.A).IsEqualTo((byte)123);
        await Assert.That(far.A).IsEqualTo((byte)123);
    }
}
