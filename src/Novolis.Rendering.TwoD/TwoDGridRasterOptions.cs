using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

/// <summary>Options for <see cref="TwoDSceneGridRasterizer"/>.</summary>
public sealed class TwoDGridRasterOptions
{
    /// <summary>Cell mapping mode.</summary>
    public TwoDGridCoordinateSpace Space { get; init; } = TwoDGridCoordinateSpace.ScreenPixels;

    /// <summary>World size of one cell when <see cref="Space"/> is <see cref="TwoDGridCoordinateSpace.WorldCells"/>.</summary>
    public float CellSize { get; init; } = 1f;

    /// <summary>Clear color written before drawing (per layer).</summary>
    public Rgba32 ClearColor { get; init; }

    /// <summary>When true, outlines on <see cref="TwoDStaticPolygon"/> are drawn on the same layer.</summary>
    public bool DrawPolygonOutlines { get; init; } = true;

    /// <summary>Outline width in cells/pixels when <see cref="DrawPolygonOutlines"/> is true.</summary>
    public int OutlineThickness { get; init; } = 1;

    /// <summary>
    /// World-space bounds for <see cref="TwoDGridCoordinateSpace.WorldCells"/>.
    /// When null, bounds are derived from the camera viewport.
    /// </summary>
    public TwoDWorldBounds? WorldBounds { get; init; }

    /// <summary>Creates screen-pixel raster options for the scene camera viewport.</summary>
    public static TwoDGridRasterOptions ScreenPixels(Rgba32 clear = default) =>
        new() { Space = TwoDGridCoordinateSpace.ScreenPixels, ClearColor = clear };

    /// <summary>Creates world-cell raster options.</summary>
    /// <param name="cellSize">World units per cell.</param>
    /// <param name="worldBounds">Optional fixed bounds; when null, derived from the camera.</param>
    /// <param name="clear">Clear color written before drawing (per layer).</param>
    public static TwoDGridRasterOptions WorldCells(float cellSize, TwoDWorldBounds? worldBounds = null, Rgba32 clear = default) =>
        new()
        {
            Space = TwoDGridCoordinateSpace.WorldCells,
            CellSize = cellSize,
            WorldBounds = worldBounds,
            ClearColor = clear,
        };
}

/// <summary>Axis-aligned world XZ bounds.</summary>
/// <param name="MinX">Left edge.</param>
/// <param name="MinZ">Bottom edge.</param>
/// <param name="MaxX">Right edge.</param>
/// <param name="MaxZ">Top edge.</param>
public readonly record struct TwoDWorldBounds(float MinX, float MinZ, float MaxX, float MaxZ);
