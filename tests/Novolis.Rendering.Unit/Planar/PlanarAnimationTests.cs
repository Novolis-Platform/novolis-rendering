using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarAnimationTests
{
    [Test]
    public async Task CurrentFrameIndex_AdvancesWithTime()
    {
        var registry = new PlanarTextureRegistry();
        var id = registry.Register(new Novolis.Math.Geometry.Rgba32[64], 8, 8);
        var sheet = new PlanarSpriteSheet(id, 4, 4, 8, 8);
        var clip = PlanarAnimationClip.FromRow(sheet, row: 0, startColumn: 0, count: 4, framesPerSecond: 10f);
        var anim = new PlanarAnimatedSprite { Clip = clip, Time = 0.25f };
        await Assert.That(anim.CurrentFrameIndex).IsEqualTo(2);
    }
}
