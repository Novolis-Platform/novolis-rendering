using Novolis.Rendering.PathTrace.Demos;
using TUnit.Core;

namespace Novolis.Rendering.PathTrace.Demos.Tests;

public sealed class InteractiveTraceSizeTests
{
    [Test]
    public async Task Clamp_Caps_Large_Windows()
    {
        var (w, h) = InteractiveTraceSize.Clamp(1920, 1080);
        await Assert.That(w).IsEqualTo(960);
        await Assert.That(h).IsEqualTo(540);
        await Assert.That(w % 2).IsEqualTo(0);
        await Assert.That(h % 2).IsEqualTo(0);
    }

    [Test]
    public async Task Clamp_Preserves_Small_Windows()
    {
        var (w, h) = InteractiveTraceSize.Clamp(800, 600);
        await Assert.That(w).IsLessThanOrEqualTo(800);
        await Assert.That(h).IsLessThanOrEqualTo(600);
        await Assert.That(w).IsLessThanOrEqualTo(InteractiveTraceSize.DefaultMaxWidth);
    }
}
