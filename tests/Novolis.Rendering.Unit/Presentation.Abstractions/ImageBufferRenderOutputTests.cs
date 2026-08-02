using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Presentation.Abstractions;
using TUnit.Core;

namespace Novolis.Rendering.Presentation.Abstractions.Tests;

public sealed class ImageBufferRenderOutputTests
{
    [Test]
    public async Task TryGetCpuPixels_ReturnsFalseWhenBufferUnset()
    {
        var output = new ImageBufferRenderOutput();
        await Assert.That(output.TryGetCpuPixels(out _, out var width, out var height)).IsFalse();
        await Assert.That(width).IsEqualTo(0);
        await Assert.That(height).IsEqualTo(0);
    }

    [Test]
    public async Task TryGetCpuPixels_ReturnsAttachedBuffer()
    {
        var buffer = new ImageBuffer(2, 1);
        buffer[0, 0] = new Rgba32(255, 0, 0, 255);
        buffer[1, 0] = new Rgba32(0, 255, 0, 255);

        var output = new ImageBufferRenderOutput { Buffer = buffer };
        var hasPixels = output.TryGetCpuPixels(out var pixels, out var width, out var height);
        var pixelCount = pixels.Length;
        var firstRed = pixels[0].R;
        await Assert.That(hasPixels).IsTrue();
        await Assert.That(width).IsEqualTo(2);
        await Assert.That(height).IsEqualTo(1);
        await Assert.That(pixelCount).IsEqualTo(2);
        await Assert.That(firstRed).IsEqualTo((byte)255);
    }

    [Test]
    public async Task Buffer_ThrowsWhenUnset()
    {
        var output = new ImageBufferRenderOutput();
        await Assert.That(() => _ = output.Buffer).Throws<InvalidOperationException>();
    }
}
