using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDSceneTessellatorTests
{
    [Test]
    public async Task Tessellate_emits_platform_triangles_and_hud_glyphs()
    {
        var scene = new TwoDScene();
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
        var scene = new TwoDScene();
        var pixels = new Rgba32[4];
        var id = scene.Textures.Register(pixels, 2, 2, "unit");
        scene.Sprites.Add(new TwoDSpriteInstance
        {
            Texture = id,
            Transform = new TwoDTransform { Position = new System.Numerics.Vector3(0f, 0f, 0f), Scale = new System.Numerics.Vector3(1f, 0f, 1f) },
        });

        var list = scene.Tessellate(64, 64);

        await Assert.That(list.Textures.Length).IsEqualTo(1);
        await Assert.That(list.Textures[0].Id).IsEqualTo(id.Value);
        await Assert.That(list.Triangles.Length).IsEqualTo(2);
    }
}
