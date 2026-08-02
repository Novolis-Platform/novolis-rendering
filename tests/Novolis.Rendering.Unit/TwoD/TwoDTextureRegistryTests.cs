using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDTextureRegistryTests
{
    [Test]
    public async Task Register_returns_monotonic_ids()
    {
        var registry = new TwoDTextureRegistry();
        var pixels = new Rgba32[4];
        pixels[0] = Rgba32.Red;

        var first = registry.Register(pixels, 2, 2, "test");
        var second = registry.Register(pixels, 2, 2);

        await Assert.That(first.Value).IsEqualTo(1);
        await Assert.That(second.Value).IsEqualTo(2);
        await Assert.That(registry.Contains(first)).IsTrue();
    }

    [Test]
    public async Task GetInfo_returns_dimensions_and_name()
    {
        var registry = new TwoDTextureRegistry();
        var id = registry.Register(new Rgba32[6], 3, 2, "icon");

        var info = registry.GetInfo(id);

        await Assert.That(info.Width).IsEqualTo(3);
        await Assert.That(info.Height).IsEqualTo(2);
        await Assert.That(info.Name).IsEqualTo("icon");
    }

    [Test]
    public async Task CopyPixels_copies_registered_buffer()
    {
        var registry = new TwoDTextureRegistry();
        var src = new Rgba32[] { Rgba32.Red, new Rgba32(0, 255, 0), new Rgba32(0, 0, 255), Rgba32.White };
        var id = registry.Register(src, 2, 2);

        var dst = new Rgba32[4];
        registry.CopyPixels(id, dst, out var w, out var h);

        await Assert.That(w).IsEqualTo(2);
        await Assert.That(h).IsEqualTo(2);
        await Assert.That(dst[0]).IsEqualTo(Rgba32.Red);
        await Assert.That(dst[3]).IsEqualTo(Rgba32.White);
    }

    [Test]
    public async Task Register_rejects_mismatched_pixel_count()
    {
        var registry = new TwoDTextureRegistry();
        await Assert.That(() => registry.Register(new Rgba32[3], 2, 2))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task GetInfo_unknown_id_throws()
    {
        var registry = new TwoDTextureRegistry();
        await Assert.That(() => registry.GetInfo(new TwoDTextureId(99)))
            .ThrowsExactly<KeyNotFoundException>();
    }
}
