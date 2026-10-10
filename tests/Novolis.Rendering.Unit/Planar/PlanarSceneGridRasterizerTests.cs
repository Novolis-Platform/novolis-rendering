using Novolis.Math.Geometry;
using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarSceneGridRasterizerTests
{
    [Test]
    public async Task WorldCells_PlatformFillsExpectedCells()
    {
        var scene = new PlanarScene();
        scene.AddPlatform(1f, 1f, 3f, 3f, new Rgba32(90, 110, 150));

        var grids = scene.ToLayeredGrids(new PlanarGridRasterOptions
        {
            Space = PlanarGridCoordinateSpace.WorldCells,
            CellSize = 1f,
            WorldBounds = new PlanarWorldBounds(0f, 0f, 6f, 6f),
            DrawPolygonOutlines = false,
        });

        var world = grids.TryGetLayer(PlanarDrawLayer.World)!;
        await Assert.That((world[2, 2, 0] is Rgba32 c ? c.A : 0)).IsGreaterThan(0);
        await Assert.That((world[0, 0, 0] is Rgba32 c0 ? c0.A : 0)).IsEqualTo(0);
        await Assert.That(grids.Space).IsEqualTo(PlanarGridCoordinateSpace.WorldCells);
    }

    [Test]
    public async Task ScreenPixels_HudStaysOnHudLayer()
    {
        var scene = new PlanarScene();
        scene.Camera.ViewportWidth = 64;
        scene.Camera.ViewportHeight = 48;
        scene.Hud.AddText("HI", 4, 4, 2f, Rgba32.White);

        var grids = scene.ToLayeredGrids(PlanarGridRasterOptions.ScreenPixels());

        var world = grids.TryGetLayer(PlanarDrawLayer.World)!;
        var hud = grids.TryGetLayer(PlanarDrawLayer.Hud)!;
        await Assert.That(CountOpaque(world)).IsEqualTo(0);
        await Assert.That(CountOpaque(hud)).IsGreaterThan(0);
        await Assert.That(CountOpaque(grids.Composited)).IsGreaterThan(0);
    }

    [Test]
    public async Task CompositedToAscii_ProducesReadableMap()
    {
        var scene = new PlanarScene();
        scene.Camera.ViewportWidth = 8;
        scene.Camera.ViewportHeight = 4;
        scene.AddPlatform(0f, 0f, 4f, 2f, Rgba32.Red);

        var grids = scene.ToLayeredGrids(PlanarGridRasterOptions.WorldCells(
            1f,
            new PlanarWorldBounds(0f, 0f, 8f, 4f)));

        var ascii = grids.ToAscii(PlanarDrawLayer.World, '#', '.');
        await Assert.That(ascii).Contains('#');
        await Assert.That(ascii.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length)
            .IsEqualTo((int)grids.Height);
    }

    private static int CountOpaque(Novolis.Math.Arrays.DenseGrid<Rgba32> grid)
    {
        var count = 0;
        for (var y = 0u; y < grid.Height; y++)
        for (var x = 0u; x < grid.Width; x++)
        {
            if (grid[x, y, 0] is Rgba32 c && c.A > 0)
            {
                count++;
            }
        }

        return count;
    }
}
