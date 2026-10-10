using Novolis.Math.Geometry;
using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarTransformTests
{
    [Test]
    public async Task PlanarSpriteInstance_defaults_to_full_source_and_white_tint()
    {
        var sprite = new PlanarSpriteInstance();

        await Assert.That(sprite.SourceRect).IsEqualTo(PlanarSourceRect.Full);
        await Assert.That(sprite.Tint).IsEqualTo(Rgba32.White);
        await Assert.That(sprite.Layer).IsEqualTo(PlanarDrawLayer.World);
    }

    [Test]
    public async Task PlanarTextureId_none_is_invalid()
    {
        await Assert.That(PlanarTextureId.None.IsValid).IsFalse();
        await Assert.That(new PlanarTextureId(1).IsValid).IsTrue();
    }
}
