using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarSourceRectTests
{
    [Test]
    public async Task Full_covers_entire_texture()
    {
        var rect = PlanarSourceRect.Full;

        await Assert.That(rect.U0).IsEqualTo(0f);
        await Assert.That(rect.V0).IsEqualTo(0f);
        await Assert.That(rect.U1).IsEqualTo(1f);
        await Assert.That(rect.V1).IsEqualTo(1f);
    }

    [Test]
    public async Task FromPixels_maps_atlas_region()
    {
        var rect = PlanarSourceRect.FromPixels(16, 8, 32, 16, textureWidth: 128, textureHeight: 64);

        await Assert.That(rect.U0).IsEqualTo(0.125f);
        await Assert.That(rect.V0).IsEqualTo(0.125f);
        await Assert.That(rect.U1).IsEqualTo(0.375f);
        await Assert.That(rect.V1).IsEqualTo(0.375f);
    }

    [Test]
    public async Task FromPixels_rejects_zero_texture_size()
    {
        await Assert.That(() => PlanarSourceRect.FromPixels(0, 0, 1, 1, 0, 64))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }
}
