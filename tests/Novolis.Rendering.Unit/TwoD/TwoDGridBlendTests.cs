using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDGridBlendTests
{
    [Test]
    public async Task SemiTransparent_sprite_blends_over_world_grid()
    {
        var scene = new TwoDScene();
        scene.Camera.ViewportWidth = 32;
        scene.Camera.ViewportHeight = 32;
        scene.AddPlatform(0f, 0f, 4f, 4f, Rgba32.Red);

        var pixels = Enumerable.Repeat(new Rgba32(0, 255, 0, 128), 64).ToArray();
        var texture = scene.Textures.Register(pixels, 8, 8);
        scene.Sprites.Add(new TwoDSpriteInstance
        {
            Texture = texture,
            Transform = { Position = new System.Numerics.Vector3(1f, 0f, 1f), Scale = new System.Numerics.Vector3(2f, 1f, 2f) },
        });

        var grids = scene.ToLayeredGrids(TwoDGridRasterOptions.ScreenPixels());
        var composited = grids.Composited;

        var opaque = 0;
        var blended = 0;
        for (var y = 0u; y < composited.Height; y++)
        for (var x = 0u; x < composited.Width; x++)
        {
            if (composited[x, y, 0] is Rgba32 c && c.A > 0)
            {
                opaque++;
                if (c is { G: > 0, R: > 0 })
                    blended++;
            }
        }

        await Assert.That(opaque).IsGreaterThan(0);
        await Assert.That(blended).IsGreaterThan(0);
    }
}
