using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarAnimatedSpriteTests
{
    static PlanarAnimationClip Clip(int count, float fps = 10f)
    {
        var registry = new PlanarTextureRegistry();
        var id = registry.Register(new Novolis.Math.Geometry.Rgba32[64], 8, 8);
        var sheet = new PlanarSpriteSheet(id, 8, 8, 8, 8);
        return PlanarAnimationClip.FromRow(sheet, 0, 0, count, fps);
    }

    [Test]
    public async Task CurrentFrameIndex_loops_when_enabled()
    {
        var sprite = new PlanarAnimatedSprite { Clip = Clip(4), Time = 0.35f, Loop = true };
        await Assert.That(sprite.CurrentFrameIndex).IsEqualTo(3);
    }

    [Test]
    public async Task CurrentFrameIndex_clamps_when_not_looping()
    {
        var sprite = new PlanarAnimatedSprite { Clip = Clip(4), Time = 99f, Loop = false };
        await Assert.That(sprite.CurrentFrameIndex).IsEqualTo(3);
    }

    [Test]
    public async Task CurrentFrameIndex_zero_when_clip_missing()
    {
        var sprite = new PlanarAnimatedSprite { Clip = null!, Time = 5f };
        await Assert.That(sprite.CurrentFrameIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Advance_and_reset_update_time()
    {
        var sprite = new PlanarAnimatedSprite { Clip = Clip(4), Time = 1f };
        sprite.Advance(0.5f);
        await Assert.That(sprite.Time).IsEqualTo(1.5f);
        sprite.Reset();
        await Assert.That(sprite.Time).IsEqualTo(0f);
    }
}
