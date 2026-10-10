using Novolis.Math.Geometry;
using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarSpriteSheetTests
{
    [Test]
    public async Task GetFrame_maps_grid_cells_to_uv_rects()
    {
        var sheet = new PlanarSpriteSheet(new PlanarTextureId(1), frameWidth: 8, frameHeight: 8, textureWidth: 32, textureHeight: 16);

        var frame0 = sheet.GetFrame(0);
        var frame1 = sheet.GetFrame(1);

        await Assert.That(sheet.Columns).IsEqualTo(4);
        await Assert.That(sheet.Rows).IsEqualTo(2);
        await Assert.That(frame0.U0).IsEqualTo(0f);
        await Assert.That(frame1.U0).IsGreaterThan(frame0.U0);
    }

    [Test]
    public async Task GetFrame_rejects_negative_index()
    {
        var sheet = new PlanarSpriteSheet(new PlanarTextureId(1), 4, 4, 8, 8);
        await Assert.That(() => sheet.GetFrame(-1)).ThrowsExactly<ArgumentOutOfRangeException>();
    }
}
