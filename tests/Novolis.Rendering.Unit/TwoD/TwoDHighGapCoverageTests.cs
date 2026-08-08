using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;
using TUnit.Core;

namespace Novolis.Rendering.Unit.TwoD;

/// <summary>Raises TwoD line/branch coverage: rasterizer paths, blend, scene, clips, layers.</summary>
public sealed class TwoDHighGapCoverageTests
{
    [Test]
    public async Task AnimationClip_RejectsEmptyAndGetSourceRectWraps()
    {
        var sheet = new TwoDSpriteSheet(new TwoDTextureId(1), 8, 8, 32, 16);
        await Assert.That(() => new TwoDAnimationClip(sheet, [], 12f))
            .ThrowsExactly<ArgumentException>();

        var clip = TwoDAnimationClip.FromRow(sheet, row: 1, startColumn: 0, count: 2, framesPerSecond: 8f);
        await Assert.That(clip.FrameCount).IsEqualTo(2);
        var rect = clip.GetSourceRect(5);
        await Assert.That(rect.U0).IsGreaterThanOrEqualTo(0f);
    }

    [Test]
    public async Task SpriteSheet_RejectsZeroColumns()
    {
        await Assert.That(() => new TwoDSpriteSheet(new TwoDTextureId(1), 16, 16, 8, 8))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Transform_PositionCtorAndFlip()
    {
        var t = new TwoDTransform(new Vector3(3, 0, 4)) { FlipX = true, RotationY = 0.5f };
        await Assert.That(t.Position.X).IsEqualTo(3f);
        await Assert.That(t.FlipX).IsTrue();
    }

    [Test]
    public async Task Scene_UpdateAdvancesAnimatedSprites()
    {
        var scene = new TwoDScene();
        scene.Camera.ViewportWidth = 64;
        scene.Camera.ViewportHeight = 48;
        scene.Camera.WorldUnitsPerPixel = 0.1f;

        var tex = scene.Textures.Register(Enumerable.Repeat(Rgba32.White, 64).ToArray(), 8, 8);
        var sheet = new TwoDSpriteSheet(tex, 4, 4, 8, 8);
        var clip = TwoDAnimationClip.FromRow(sheet, 0, 0, 4, framesPerSecond: 10f);
        var anim = new TwoDAnimatedSprite { Clip = clip, Time = 0f, Loop = false };
        scene.AnimatedSprites.Add(anim);
        scene.Update(0.35f);
        await Assert.That(anim.CurrentFrameIndex).IsEqualTo(3);
        anim.Reset();
        await Assert.That(anim.Time).IsEqualTo(0f);
    }

    [Test]
    public async Task LayerGridSet_MissingLayerAndCompositedAscii()
    {
        var scene = new TwoDScene();
        scene.Camera.ViewportWidth = 16;
        scene.Camera.ViewportHeight = 8;
        scene.AddPlatform(0f, 0f, 2f, 2f, Rgba32.Red);

        var grids = scene.ToLayeredGrids(TwoDGridRasterOptions.ScreenPixels());
        await Assert.That(grids.Layers.Count).IsGreaterThan(0);
        await Assert.That(grids.CompositedToAscii('#', '.')).Contains('#');

        // Manually-built set without Foreground exercises TryGetLayer miss + ToAscii empty.
        var sparse = new TwoDLayerGridSet
        {
            Width = 4,
            Height = 2,
            Composited = grids.Composited,
        };
        await Assert.That(sparse.TryGetLayer(TwoDDrawLayer.Foreground)).IsNull();
        await Assert.That(sparse.ToAscii(TwoDDrawLayer.Foreground)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Rasterizer_DeriveWorldBounds_WithoutExplicitBounds()
    {
        var scene = new TwoDScene();
        scene.Camera.ViewportWidth = 40;
        scene.Camera.ViewportHeight = 30;
        scene.Camera.WorldUnitsPerPixel = 0.25f;
        scene.Camera.Position = new Vector3(5, 0, 5);
        scene.AddPlatform(4f, 4f, 6f, 6f, new Rgba32(20, 40, 60));

        var grids = scene.ToLayeredGrids(new TwoDGridRasterOptions
        {
            Space = TwoDGridCoordinateSpace.WorldCells,
            CellSize = 1f,
            WorldBounds = null,
            DrawPolygonOutlines = true,
            OutlineThickness = 1,
        });

        await Assert.That(grids.Width).IsGreaterThan(0u);
        await Assert.That(CountOpaque(grids.Composited)).IsGreaterThan(0);
    }

    [Test]
    public async Task Rasterizer_ScreenSprite_HudSprite_Menu_AndBlendPaths()
    {
        var scene = new TwoDScene();
        scene.Camera.ViewportWidth = 80;
        scene.Camera.ViewportHeight = 60;

        var opaque = Enumerable.Repeat(new Rgba32(255, 0, 0, 255), 16).ToArray();
        var translucent = Enumerable.Repeat(new Rgba32(0, 255, 0, 100), 16).ToArray();
        var transparent = Enumerable.Repeat(new Rgba32(0, 0, 255, 0), 16).ToArray();
        var texOpaque = scene.Textures.Register(opaque, 4, 4);
        var texTrans = scene.Textures.Register(translucent, 4, 4);
        _ = scene.Textures.Register(transparent, 4, 4);

        scene.Sprites.Add(new TwoDSpriteInstance
        {
            Texture = texOpaque,
            ScreenSpace = true,
            Transform = { Position = new Vector3(10, 0, 10), Scale = new Vector3(20, 1, 12) },
            Tint = new Rgba32(255, 0, 0, 255),
        });
        scene.Sprites.Add(new TwoDSpriteInstance
        {
            Texture = texTrans,
            Transform = { Position = new Vector3(2, 0, 2), Scale = new Vector3(2, 1, 2) },
            Tint = new Rgba32(0, 255, 0, 128),
        });
        scene.Sprites.Add(new TwoDSpriteInstance
        {
            Texture = texOpaque,
            Transform = { Position = new Vector3(3, 0, 3), Scale = new Vector3(1, 1, 1) },
            Tint = new Rgba32(0, 0, 0, 0),
        });

        var sheet = new TwoDSpriteSheet(texOpaque, 2, 2, 4, 4);
        var clip = new TwoDAnimationClip(sheet, [0, 1], 12f);
        scene.AnimatedSprites.Add(new TwoDAnimatedSprite
        {
            Clip = clip,
            Transform = { Position = new Vector3(1, 0, 1), Scale = new Vector3(1.5f, 1, 1.5f) },
            Time = 0.1f,
        });

        scene.Hud.AddSprite(texOpaque, TwoDSourceRect.Full, 2, 2, 16, 16);
        scene.Hud.AddText("A B", 4, 30, 2f, Rgba32.White);

        scene.Menus.Push(new TwoDMenuScreen("Pause",
        [
            new TwoDMenuItem("Resume", "resume", () => "resume"),
            new TwoDMenuItem("Quit", "quit", () => "quit"),
        ]));
        scene.Menus.Navigate(1);

        var grids = scene.ToLayeredGrids(TwoDGridRasterOptions.ScreenPixels());
        await Assert.That(grids.TryGetLayer(TwoDDrawLayer.Hud)).IsNotNull();
        await Assert.That(grids.TryGetLayer(TwoDDrawLayer.Menu)).IsNotNull();
        await Assert.That(CountOpaque(grids.Composited)).IsGreaterThan(10);
        await Assert.That(scene.Menus.Select()).IsEqualTo("quit");
    }

    [Test]
    public async Task Rasterizer_ScreenSpacePlatformOutline_AndHudDefaultColor()
    {
        var scene = new TwoDScene();
        scene.Camera.ViewportWidth = 48;
        scene.Camera.ViewportHeight = 36;
        scene.AddPlatform(0f, 0f, 3f, 2f, new Rgba32(90, 90, 200, 200));
        scene.Hud.AddText("Hi", 1, 1);

        var grids = scene.ToLayeredGrids(new TwoDGridRasterOptions
        {
            Space = TwoDGridCoordinateSpace.ScreenPixels,
            DrawPolygonOutlines = true,
            OutlineThickness = 1,
            ClearColor = new Rgba32(10, 10, 10, 255),
        });

        await Assert.That(CountOpaque(grids.TryGetLayer(TwoDDrawLayer.Hud)!)).IsGreaterThan(0);
    }

    [Test]
    public async Task TextureRegistry_RejectsBadSizeAndSmallCopyBuffer()
    {
        var registry = new TwoDTextureRegistry();
        await Assert.That(() => registry.Register(new Rgba32[1], 0, 1))
            .ThrowsExactly<ArgumentOutOfRangeException>();

        var id = registry.Register(new Rgba32[4], 2, 2);
        await Assert.That(() => registry.CopyPixels(id, new Rgba32[1], out _, out _))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task CollisionWorld_ClearAndZeroAxisMove()
    {
        var world = new TwoDCollisionWorld();
        world.AddStatic(new TwoDCollider(TwoDScenePrimitives.Rectangle(0, 0, 2, 2)));
        await Assert.That(world.StaticColliders.Count).IsEqualTo(1);
        var same = world.MoveCircle(new Vector3(5, 0, 5), Vector3.Zero, 0.3f);
        await Assert.That(same).IsEqualTo(new Vector3(5, 0, 5));
        world.Clear();
        await Assert.That(world.StaticColliders.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PlanarCollision_ClosestOnDegenerateEdge()
    {
        var poly = new Novolis.Math.Topology.Polygon(
        [
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
        ]);
        await Assert.That(TwoDPlanarCollision.CircleOverlaps(poly, 0f, 0f, 0.1f)).IsTrue();
        await Assert.That(TwoDPlanarCollision.TryGetCircleSeparation(poly, 10f, 10f, 0.1f, out _)).IsFalse();
    }

    [Test]
    public async Task MenuStack_EmptyNavigateAndPop()
    {
        var stack = new TwoDMenuStack();
        stack.Navigate(1);
        await Assert.That(stack.Select()).IsNull();
        await Assert.That(stack.Pop()).IsFalse();
        stack.Push(new TwoDMenuScreen("Empty", []));
        stack.Navigate(1);
        await Assert.That(stack.Select()).IsNull();
        await Assert.That(stack.Pop()).IsTrue();
        stack.Clear();
        await Assert.That(stack.IsActive).IsFalse();
    }

    private static int CountOpaque(Novolis.Math.Arrays.DenseGrid<Rgba32> grid)
    {
        var count = 0;
        for (var y = 0u; y < grid.Height; y++)
        for (var x = 0u; x < grid.Width; x++)
        {
            if (grid[x, y, 0] is Rgba32 c && c.A > 0)
                count++;
        }

        return count;
    }
}
