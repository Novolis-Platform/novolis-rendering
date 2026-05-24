using Novolis.Rendering.Presentation.Silk;
using TUnit.Core;

namespace Novolis.Rendering.Presentation.Silk.Tests;

public sealed class SilkSmoothedFpsTests
{
    [Test]
    public async Task Update_produces_positive_fps()
    {
        var fps = new SilkSmoothedFps();
        fps.Update(1f / 60f);
        await Assert.That(fps.Value).IsGreaterThan(0f);
    }
}
