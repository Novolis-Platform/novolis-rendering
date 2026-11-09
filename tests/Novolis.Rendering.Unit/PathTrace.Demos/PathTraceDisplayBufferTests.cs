using Novolis.Math.Geometry;
using Novolis.Rendering.PathTrace.Demos;
using TUnit.Core;

namespace Novolis.Rendering.PathTrace.Demos.Tests;

public sealed class PathTraceDisplayBufferTests
{
    [Test]
    public async Task TryCopyFrameIfNew_Skips_Until_Publish()
    {
        var display = new PathTraceDisplayBuffer();
        display.Invalidate(4, 2);
        var gen = -1;
        await Assert.That(display.TryCopyFrameIfNew(ref gen, out _, out _, out _)).IsTrue(); // cleared frame once
        await Assert.That(display.TryCopyFrameIfNew(ref gen, out _, out _, out _)).IsFalse();

        var pixels = new Rgba32[8];
        pixels[0] = Rgba32.Red;
        display.Publish(pixels, 4, 2, sampleCount: 3);
        await Assert.That(display.TryCopyFrameIfNew(ref gen, out var copy, out var w, out var h)).IsTrue();
        await Assert.That(w).IsEqualTo(4);
        await Assert.That(h).IsEqualTo(2);
        await Assert.That(copy[0]).IsEqualTo(Rgba32.Red);
        await Assert.That(display.TryCopyFrameIfNew(ref gen, out _, out _, out _)).IsFalse();
    }
}
