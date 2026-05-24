using Novolis.Math.Geometry;
using Novolis.Rendering.Presentation.Silk;
using TUnit.Core;

namespace Novolis.Rendering.Presentation.Silk.Tests;

public sealed class SilkCpuFramePresenterTests
{
    [Test]
    public async Task PresentCpuFrame_ForwardsToSink()
    {
        Rgba32[]? captured = null;
        var width = 0;
        var height = 0;
        var presenter = new SilkCpuFramePresenter((pixels, w, h) =>
        {
            captured = pixels.ToArray();
            width = w;
            height = h;
        });

        var frame = new Rgba32[] { new(255, 0, 0, 255), new(0, 255, 0, 255) };
        presenter.PresentCpuFrame(frame, 2, 1);

        await Assert.That(captured).IsNotNull();
        await Assert.That(width).IsEqualTo(2);
        await Assert.That(height).IsEqualTo(1);
        await Assert.That(captured![0].R).IsEqualTo((byte)255);
    }
}
