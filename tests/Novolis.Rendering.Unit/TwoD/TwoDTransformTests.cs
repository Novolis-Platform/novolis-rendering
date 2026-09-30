using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDTransformTests
{
    [Test]
    public async Task TwoDSpriteInstance_defaults_to_full_source_and_white_tint()
    {
        var sprite = new TwoDSpriteInstance();

        await Assert.That(sprite.SourceRect).IsEqualTo(TwoDSourceRect.Full);
        await Assert.That(sprite.Tint).IsEqualTo(Rgba32.White);
        await Assert.That(sprite.Layer).IsEqualTo(TwoDDrawLayer.World);
    }

    [Test]
    public async Task TwoDTextureId_none_is_invalid()
    {
        await Assert.That(TwoDTextureId.None.IsValid).IsFalse();
        await Assert.That(new TwoDTextureId(1).IsValid).IsTrue();
    }
}
