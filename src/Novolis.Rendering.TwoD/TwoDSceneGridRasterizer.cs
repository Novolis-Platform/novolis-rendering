using System.Numerics;
using Novolis.Math.Arrays;
using Novolis.Math.Geometry;
using TopologyPolygon = Novolis.Math.Topology.Polygon;

namespace Novolis.Rendering.TwoD;

/// <summary>CPU rasterizer: projects a <see cref="TwoDScene"/> into per-layer <see cref="TwoDLayerGridSet"/> buffers.</summary>
public static class TwoDSceneGridRasterizer
{
    private static readonly TwoDDrawLayer[] LayerOrder =
    [
        TwoDDrawLayer.Background,
        TwoDDrawLayer.World,
        TwoDDrawLayer.Foreground,
        TwoDDrawLayer.Hud,
        TwoDDrawLayer.Menu,
    ];

    /// <summary>Rasterizes <paramref name="scene"/> into layered in-memory grids.</summary>
    public static TwoDLayerGridSet Rasterize(TwoDScene scene, TwoDGridRasterOptions options)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(options);

        var (width, height, worldMinX, worldMinZ) = ResolveDimensions(scene, options);
        var layers = CreateLayers(width, height, options.ClearColor);
        var drawables = CollectDrawables(scene);

        foreach (var item in drawables.OrderBy(d => d.Layer).ThenBy(d => d.SortKey))
        {
            RasterizeDrawable(item, scene, options, layers, width, height, worldMinX, worldMinZ);
        }

        var composited = Compose(layers, width, height);
        var result = new TwoDLayerGridSet
        {
            Width = width,
            Height = height,
            Space = options.Space,
            CellSize = options.CellSize,
            WorldMinX = worldMinX,
            WorldMinZ = worldMinZ,
            Composited = composited,
        };

        foreach (var pair in layers)
        {
            result.SetLayer(pair.Key, pair.Value);
        }

        return result;
    }

    private static (uint Width, uint Height, float WorldMinX, float WorldMinZ) ResolveDimensions(
        TwoDScene scene,
        TwoDGridRasterOptions options)
    {
        if (options.Space == TwoDGridCoordinateSpace.ScreenPixels)
        {
            var w = (uint)System.Math.Max(1, scene.Camera.ViewportWidth);
            var h = (uint)System.Math.Max(1, scene.Camera.ViewportHeight);
            return (w, h, 0f, 0f);
        }

        var bounds = options.WorldBounds ?? DeriveWorldBounds(scene);
        var cell = System.Math.Max(1e-6f, options.CellSize);
        var width = (uint)System.Math.Max(1, MathF.Ceiling((bounds.MaxX - bounds.MinX) / cell));
        var height = (uint)System.Math.Max(1, MathF.Ceiling((bounds.MaxZ - bounds.MinZ) / cell));
        return (width, height, bounds.MinX, bounds.MinZ);
    }

    private static TwoDWorldBounds DeriveWorldBounds(TwoDScene scene)
    {
        var halfW = scene.Camera.ViewportWidth * 0.5f * scene.Camera.WorldUnitsPerPixel;
        var halfH = scene.Camera.ViewportHeight * 0.5f * scene.Camera.WorldUnitsPerPixel;
        return new TwoDWorldBounds(
            scene.Camera.Position.X - halfW,
            scene.Camera.Position.Z - halfH,
            scene.Camera.Position.X + halfW,
            scene.Camera.Position.Z + halfH);
    }

    private static Dictionary<TwoDDrawLayer, DenseGrid<Rgba32>> CreateLayers(
        uint width,
        uint height,
        Rgba32 clear)
    {
        var layers = new Dictionary<TwoDDrawLayer, DenseGrid<Rgba32>>();
        foreach (var layer in LayerOrder)
        {
            var grid = new DenseGrid<Rgba32>(width, height);
            for (var y = 0u; y < height; y++)
            for (var x = 0u; x < width; x++)
            {
                grid[x, y, 0] = clear;
            }

            layers[layer] = grid;
        }

        return layers;
    }

    private static List<GridDrawable> CollectDrawables(TwoDScene scene)
    {
        var list = new List<GridDrawable>();

        foreach (var poly in scene.StaticPolygons)
        {
            list.Add(new GridDrawable(
                poly.Layer,
                poly.SortKey,
                GridDrawableKind.FilledPolygon,
                poly.Shape,
                poly.FillColor,
                poly.OutlineColor,
                poly.DrawFilled,
                poly.DrawOutline,
                Transform: null,
                Texture: default,
                SourceRect: default,
                ScreenX: 0f,
                ScreenY: 0f,
                ScreenW: 0f,
                ScreenH: 0f,
                HudText: null));
        }

        foreach (var sprite in scene.Sprites)
        {
            if (sprite.ScreenSpace)
            {
                list.Add(new GridDrawable(
                    sprite.Layer,
                    sprite.SortKey,
                    GridDrawableKind.ScreenSprite,
                    Shape: null,
                    sprite.Tint,
                    Outline: default,
                    Fill: true,
                    OutlineDraw: false,
                    Transform: null,
                    Texture: default,
                    SourceRect: default,
                    ScreenX: sprite.Transform.Position.X,
                    ScreenY: sprite.Transform.Position.Z,
                    ScreenW: sprite.Transform.Scale.X,
                    ScreenH: sprite.Transform.Scale.Z,
                    HudText: null));
            }
            else
            {
                list.Add(new GridDrawable(
                    sprite.Layer,
                    sprite.SortKey,
                    GridDrawableKind.WorldSprite,
                    Shape: null,
                    sprite.Tint,
                    Outline: default,
                    Fill: true,
                    OutlineDraw: false,
                    sprite.Transform,
                    sprite.Texture,
                    sprite.SourceRect,
                    ScreenX: 0f,
                    ScreenY: 0f,
                    ScreenW: 0f,
                    ScreenH: 0f,
                    HudText: null));
            }
        }

        foreach (var anim in scene.AnimatedSprites)
        {
            var rect = anim.Clip.Sheet.GetFrame(anim.CurrentFrameIndex);
            list.Add(new GridDrawable(
                anim.Layer,
                anim.SortKey,
                GridDrawableKind.WorldSprite,
                Shape: null,
                Rgba32.White,
                Outline: default,
                Fill: true,
                OutlineDraw: false,
                anim.Transform,
                anim.Clip.Sheet.Texture,
                rect,
                ScreenX: 0f,
                ScreenY: 0f,
                ScreenW: 0f,
                ScreenH: 0f,
                HudText: null));
        }

        foreach (var element in scene.Hud.Elements)
        {
            if (element is TwoDHudText text)
            {
                list.Add(new GridDrawable(
                    TwoDDrawLayer.Hud,
                    0,
                    GridDrawableKind.HudText,
                    Shape: null,
                    text.Color,
                    Outline: default,
                    Fill: true,
                    OutlineDraw: false,
                    Transform: null,
                    Texture: default,
                    SourceRect: default,
                ScreenX: text.ScreenX,
                ScreenY: text.ScreenY,
                ScreenW: text.Scale,
                ScreenH: 0f,
                HudText: text.Text));
            }
            else if (element is TwoDHudSprite hudSprite)
            {
                list.Add(new GridDrawable(
                    TwoDDrawLayer.Hud,
                    0,
                    GridDrawableKind.ScreenSprite,
                    Shape: null,
                    hudSprite.Tint,
                    Outline: default,
                    Fill: true,
                    OutlineDraw: false,
                    Transform: null,
                    hudSprite.Texture,
                    hudSprite.Source,
                    ScreenX: hudSprite.ScreenX,
                    ScreenY: hudSprite.ScreenY,
                    ScreenW: hudSprite.Width,
                    ScreenH: hudSprite.Height,
                    HudText: null));
            }
        }

        if (scene.Menus.Active is { } menu)
        {
            list.Add(new GridDrawable(
                TwoDDrawLayer.Menu,
                0,
                GridDrawableKind.MenuDim,
                Shape: null,
                new Rgba32(0, 0, 0, 160),
                Outline: default,
                Fill: true,
                OutlineDraw: false,
                Transform: null,
                Texture: default,
                SourceRect: default,
                ScreenX: 0f,
                ScreenY: 0f,
                ScreenW: 0f,
                ScreenH: 0f,
                HudText: null));

            list.Add(new GridDrawable(
                TwoDDrawLayer.Menu,
                1,
                GridDrawableKind.HudText,
                Shape: null,
                Rgba32.White,
                Outline: default,
                Fill: true,
                OutlineDraw: false,
                Transform: null,
                Texture: default,
                SourceRect: default,
                ScreenX: scene.Camera.ViewportWidth * 0.5f - menu.Title.Length * 6f,
                ScreenY: 80f,
                ScreenW: 3f,
                ScreenH: 0f,
                HudText: menu.Title));

            for (var i = 0; i < menu.Items.Count; i++)
            {
                var item = menu.Items[i];
                var color = i == menu.FocusIndex ? Rgba32.Chartreuse : Rgba32.White;
                list.Add(new GridDrawable(
                    TwoDDrawLayer.Menu,
                    10 + i,
                    GridDrawableKind.HudText,
                    Shape: null,
                    color,
                    Outline: default,
                    Fill: true,
                    OutlineDraw: false,
                    Transform: null,
                    Texture: default,
                    SourceRect: default,
                    ScreenX: scene.Camera.ViewportWidth * 0.5f - item.Label.Length * 6f,
                    ScreenY: 120f + i * 28f,
                    ScreenW: 2.5f,
                    ScreenH: 0f,
                    HudText: item.Label));
            }
        }

        return list;
    }

    private static void RasterizeDrawable(
        GridDrawable item,
        TwoDScene scene,
        TwoDGridRasterOptions options,
        Dictionary<TwoDDrawLayer, DenseGrid<Rgba32>> layers,
        uint width,
        uint height,
        float worldMinX,
        float worldMinZ)
    {
        var grid = layers[item.Layer];

        switch (item.Kind)
        {
            case GridDrawableKind.FilledPolygon:
                if (item.Shape is not null)
                {
                    if (item.Fill)
                    {
                        FillPolygon(grid, item.Shape, item.FillColor, scene, options, width, height, worldMinX, worldMinZ);
                    }

                    if (options.DrawPolygonOutlines && item.OutlineDraw && item.Shape is not null)
                    {
                        StrokePolygon(grid, item.Shape, item.Outline, options.OutlineThickness, scene, options, width, height, worldMinX, worldMinZ);
                    }
                }

                break;

            case GridDrawableKind.WorldSprite:
                FillWorldSprite(grid, scene, item, options, width, height, worldMinX, worldMinZ);
                break;

            case GridDrawableKind.ScreenSprite:
                FillScreenRect(
                    grid,
                    item.FillColor,
                    (int)item.ScreenX,
                    (int)item.ScreenY,
                    (int)item.ScreenW,
                    (int)item.ScreenH,
                    width,
                    height);
                break;

            case GridDrawableKind.HudText:
                FillHudText(grid, item.FillColor, item.ScreenX, item.ScreenY, item.ScreenW, item.HudText ?? string.Empty, width, height);
                break;

            case GridDrawableKind.MenuDim:
                FillScreenRect(grid, item.FillColor, 0, 0, (int)width, (int)height, width, height);
                break;
        }
    }

    private static void FillPolygon(
        DenseGrid<Rgba32> grid,
        TopologyPolygon shape,
        Rgba32 color,
        TwoDScene scene,
        TwoDGridRasterOptions options,
        uint width,
        uint height,
        float worldMinX,
        float worldMinZ)
    {
        if (!TryGetBounds(shape, out var minX, out var minZ, out var maxX, out var maxZ))
        {
            return;
        }

        if (options.Space == TwoDGridCoordinateSpace.WorldCells)
        {
            var cell = options.CellSize;
            var x0 = (uint)System.Math.Max(0, MathF.Floor((minX - worldMinX) / cell));
            var z0 = (uint)System.Math.Max(0, MathF.Floor((minZ - worldMinZ) / cell));
            var x1 = (uint)System.Math.Min(width - 1, MathF.Floor((maxX - worldMinX) / cell));
            var z1 = (uint)System.Math.Min(height - 1, MathF.Floor((maxZ - worldMinZ) / cell));
            for (var gz = z0; gz <= z1; gz++)
            for (var gx = x0; gx <= x1; gx++)
            {
                var wx = worldMinX + (gx + 0.5f) * cell;
                var wz = worldMinZ + (gz + 0.5f) * cell;
                if (TwoDPlanarCollision.ContainsPoint(shape, wx, wz))
                {
                    var cellColor = ReadCell(grid, gx, gz);
                    TwoDGridBlend.Over(ref cellColor, color);
                    WriteCell(grid, gx, gz, cellColor);
                }
            }

            return;
        }

        for (var py = 0u; py < height; py++)
        for (var px = 0u; px < width; px++)
        {
            var world = scene.Camera.ScreenToWorld(px + 0.5f, py + 0.5f);
            if (TwoDPlanarCollision.ContainsPoint(shape, world.X, world.Z))
            {
                var cellColor = ReadCell(grid, px, py);
                TwoDGridBlend.Over(ref cellColor, color);
                WriteCell(grid, px, py, cellColor);
            }
        }
    }

    private static void StrokePolygon(
        DenseGrid<Rgba32> grid,
        TopologyPolygon shape,
        Rgba32 color,
        int thickness,
        TwoDScene scene,
        TwoDGridRasterOptions options,
        uint width,
        uint height,
        float worldMinX,
        float worldMinZ)
    {
        for (var i = 0; i < shape.Length; i++)
        {
            var a = shape[i];
            var b = shape[(i + 1) % shape.Length];
            StrokeLine(grid, a, b, color, thickness, scene, options, width, height, worldMinX, worldMinZ);
        }
    }

    private static void StrokeLine(
        DenseGrid<Rgba32> grid,
        Vector3 a,
        Vector3 b,
        Rgba32 color,
        int thickness,
        TwoDScene scene,
        TwoDGridRasterOptions options,
        uint width,
        uint height,
        float worldMinX,
        float worldMinZ)
    {
        var steps = (int)(Vector3.Distance(a, b) / (options.Space == TwoDGridCoordinateSpace.WorldCells ? options.CellSize * 0.5f : scene.Camera.WorldUnitsPerPixel)) + 1;
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)System.Math.Max(1, steps);
            var p = Vector3.Lerp(a, b, t);
            PlotPoint(grid, p, color, thickness, scene, options, width, height, worldMinX, worldMinZ);
        }
    }

    private static void PlotPoint(
        DenseGrid<Rgba32> grid,
        Vector3 world,
        Rgba32 color,
        int thickness,
        TwoDScene scene,
        TwoDGridRasterOptions options,
        uint width,
        uint height,
        float worldMinX,
        float worldMinZ)
    {
        if (options.Space == TwoDGridCoordinateSpace.WorldCells)
        {
            var cell = options.CellSize;
            var gx = (int)((world.X - worldMinX) / cell);
            var gz = (int)((world.Z - worldMinZ) / cell);
            for (var dz = -thickness; dz <= thickness; dz++)
            for (var dx = -thickness; dx <= thickness; dx++)
            {
                var x = gx + dx;
                var z = gz + dz;
                if (x < 0 || z < 0 || x >= width || z >= height)
                {
                    continue;
                }

                var cellColor = ReadCell(grid, (uint)x, (uint)z);
                TwoDGridBlend.Over(ref cellColor, color);
                WriteCell(grid, (uint)x, (uint)z, cellColor);
            }

            return;
        }

        var screen = scene.Camera.WorldToScreen(world);
        var px = (int)screen.X;
        var py = (int)screen.Z;
        for (var dy = -thickness; dy <= thickness; dy++)
        for (var dx = -thickness; dx <= thickness; dx++)
        {
            FillScreenRect(grid, color, px + dx, py + dy, 1, 1, width, height);
        }
    }

    private static void FillWorldSprite(
        DenseGrid<Rgba32> grid,
        TwoDScene scene,
        GridDrawable item,
        TwoDGridRasterOptions options,
        uint width,
        uint height,
        float worldMinX,
        float worldMinZ)
    {
        if (item.Transform is null)
        {
            return;
        }

        var tint = item.FillColor;
        var halfX = 0.5f * item.Transform.Scale.X;
        var halfZ = 0.5f * item.Transform.Scale.Z;
        var poly = TwoDScenePrimitives.Rectangle(
            item.Transform.Position.X - halfX,
            item.Transform.Position.Z - halfZ,
            item.Transform.Position.X + halfX,
            item.Transform.Position.Z + halfZ);
        FillPolygon(grid, poly, tint, scene, options, width, height, worldMinX, worldMinZ);
    }

    private static void FillScreenRect(
        DenseGrid<Rgba32> grid,
        Rgba32 color,
        int x,
        int y,
        int w,
        int h,
        uint gridWidth,
        uint gridHeight)
    {
        var x1 = System.Math.Clamp(x, 0, (int)gridWidth);
        var y1 = System.Math.Clamp(y, 0, (int)gridHeight);
        var x2 = System.Math.Clamp(x + w, 0, (int)gridWidth);
        var y2 = System.Math.Clamp(y + h, 0, (int)gridHeight);
        for (var py = y1; py < y2; py++)
        for (var px = x1; px < x2; px++)
        {
            var cellColor = ReadCell(grid, (uint)px, (uint)py);
            TwoDGridBlend.Over(ref cellColor, color);
            WriteCell(grid, (uint)px, (uint)py, cellColor);
        }
    }

    private static void FillHudText(
        DenseGrid<Rgba32> grid,
        Rgba32 color,
        float screenX,
        float screenY,
        float scale,
        string text,
        uint width,
        uint height)
    {
        var charW = System.Math.Max(4, (int)(5f * scale));
        var charH = System.Math.Max(7, (int)(7f * scale));
        var x = (int)screenX;
        var y = (int)screenY;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ')
            {
                continue;
            }

            FillScreenRect(grid, color, x + i * charW, y, charW, charH, width, height);
        }
    }

    private static DenseGrid<Rgba32> Compose(
        Dictionary<TwoDDrawLayer, DenseGrid<Rgba32>> layers,
        uint width,
        uint height)
    {
        var composited = new DenseGrid<Rgba32>(width, height);
        for (var y = 0u; y < height; y++)
        for (var x = 0u; x < width; x++)
        {
            composited[x, y, 0] = default;
        }

        foreach (var layer in LayerOrder)
        {
            if (!layers.TryGetValue(layer, out var src))
            {
                continue;
            }

            for (var y = 0u; y < height; y++)
            for (var x = 0u; x < width; x++)
            {
                var dst = ReadCell(composited, x, y);
                var s = ReadCell(src, x, y);
                TwoDGridBlend.Over(ref dst, s);
                WriteCell(composited, x, y, dst);
            }
        }

        return composited;
    }

    private static bool TryGetBounds(TopologyPolygon shape, out float minX, out float minZ, out float maxX, out float maxZ)
    {
        minX = minZ = float.MaxValue;
        maxX = maxZ = float.MinValue;
        if (shape.Length == 0)
        {
            return false;
        }

        foreach (var v in shape)
        {
            minX = System.Math.Min(minX, v.X);
            maxX = System.Math.Max(maxX, v.X);
            minZ = System.Math.Min(minZ, v.Z);
            maxZ = System.Math.Max(maxZ, v.Z);
        }

        return true;
    }

    private enum GridDrawableKind
    {
        FilledPolygon,
        WorldSprite,
        ScreenSprite,
        HudText,
        MenuDim,
    }

    private sealed record GridDrawable(
        TwoDDrawLayer Layer,
        int SortKey,
        GridDrawableKind Kind,
        TopologyPolygon? Shape,
        Rgba32 FillColor,
        Rgba32 Outline,
        bool Fill,
        bool OutlineDraw,
        TwoDTransform? Transform,
        TwoDTextureId Texture,
        TwoDSourceRect SourceRect,
        float ScreenX,
        float ScreenY,
        float ScreenW,
        float ScreenH,
        string? HudText);

    private static Rgba32 ReadCell(DenseGrid<Rgba32> grid, uint x, uint y) =>
        grid[x, y, 0] is Rgba32 c ? c : default;

    private static void WriteCell(DenseGrid<Rgba32> grid, uint x, uint y, Rgba32 color) =>
        grid[x, y, 0] = color;
}
