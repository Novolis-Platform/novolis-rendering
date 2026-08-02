using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDHudTests
{
    [Test]
    public async Task AddText_and_AddSprite_register_hud_elements()
    {
        var hud = new TwoDHud();
        var text = hud.AddText("Score: 10", 8f, 12f, scale: 2f, new Rgba32(255, 255, 0));
        var pixels = new Rgba32[4];
        var texture = new TwoDTextureId(1);
        var sprite = hud.AddSprite(texture, TwoDSourceRect.Full, 0f, 0f, 32f, 32f);

        await Assert.That(hud.Elements.Count).IsEqualTo(2);
        await Assert.That(text.Text).IsEqualTo("Score: 10");
        await Assert.That(text.Color).IsEqualTo(new Rgba32(255, 255, 0));
        await Assert.That(sprite.Texture).IsEqualTo(texture);
        await Assert.That(sprite.Width).IsEqualTo(32f);
    }
}
