using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDAnimationTests
{
    [Test]
    public async Task CurrentFrameIndex_AdvancesWithTime()
    {
        var registry = new TwoDTextureRegistry();
        var id = registry.Register(new Novolis.Math.Geometry.Rgba32[64], 8, 8);
        var sheet = new TwoDSpriteSheet(id, 4, 4, 8, 8);
        var clip = TwoDAnimationClip.FromRow(sheet, row: 0, startColumn: 0, count: 4, framesPerSecond: 10f);
        var anim = new TwoDAnimatedSprite { Clip = clip, Time = 0.25f };
        await Assert.That(anim.CurrentFrameIndex).IsEqualTo(2);
    }
}
