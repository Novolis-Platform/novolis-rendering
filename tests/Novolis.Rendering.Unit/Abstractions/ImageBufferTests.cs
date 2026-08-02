using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;
using TUnit.Core;

namespace Novolis.Rendering.Abstractions.Tests;

public sealed class ImageBufferTests
{
    [Test]
    public async Task Constructor_RejectsNonPositiveDimensions()
    {
        await Assert.That(() => _ = new ImageBuffer(0, 1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => _ = new ImageBuffer(1, 0)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Index_MapsRowMajorCoordinates()
    {
        var buffer = new ImageBuffer(4, 3);
        await Assert.That(buffer.Index(2, 1)).IsEqualTo(6);
    }

    [Test]
    public async Task Clear_FillsEveryPixel()
    {
        var buffer = new ImageBuffer(2, 2);
        var color = new Rgba32(10, 20, 30, 255);
        buffer.Clear(color);

        var pixels = buffer.Pixels;
        await Assert.That(pixels.All(p => p.Equals(color))).IsTrue();
    }

    [Test]
    public async Task Indexer_ReadsAndWritesPixel()
    {
        var buffer = new ImageBuffer(2, 1);
        var color = new Rgba32(1, 2, 3, 4);
        buffer[1, 0] = color;
        await Assert.That(buffer[1, 0]).IsEqualTo(color);
    }
}
