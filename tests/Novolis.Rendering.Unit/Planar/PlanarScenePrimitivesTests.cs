using Novolis.Math.Geometry;
using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarScenePrimitivesTests
{
    [Test]
    public async Task CreateSpriteSheet_uses_texture_dimensions()
    {
        var scene = new PlanarScene();
        var pixels = new Rgba32[16];
        var texture = scene.Textures.Register(pixels, 4, 4);

        var sheet = PlanarScenePrimitives.CreateSpriteSheet(scene, texture, frameWidth: 2, frameHeight: 2);

        await Assert.That(sheet.Texture).IsEqualTo(texture);
        await Assert.That(sheet.FrameWidth).IsEqualTo(2);
        await Assert.That(sheet.FrameHeight).IsEqualTo(2);
    }

    [Test]
    public async Task AddBackground_adds_sprite_to_scene()
    {
        var scene = new PlanarScene();
        var pixels = new Rgba32[4];
        var texture = scene.Textures.Register(pixels, 2, 2);

        var sprite = PlanarScenePrimitives.AddBackground(scene, texture, worldWidth: 10f, worldHeight: 8f);

        await Assert.That(scene.Sprites).Contains(sprite);
        await Assert.That(sprite.Layer).IsEqualTo(PlanarDrawLayer.Background);
        await Assert.That(sprite.Transform.Scale.X).IsEqualTo(10f);
    }
}
