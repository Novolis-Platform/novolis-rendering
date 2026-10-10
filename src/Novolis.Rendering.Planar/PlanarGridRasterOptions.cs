using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Options for <see cref="PlanarSceneGridRasterizer"/>.</summary>
public sealed class PlanarGridRasterOptions
{
    /// <summary>Cell mapping mode.</summary>
    public PlanarGridCoordinateSpace Space { get; init; } = PlanarGridCoordinateSpace.ScreenPixels;

    /// <summary>World size of one cell when <see cref="Space"/> is <see cref="PlanarGridCoordinateSpace.WorldCells"/>.</summary>
    public float CellSize { get; init; } = 1f;

    /// <summary>Clear color written before drawing (per layer).</summary>
    public Rgba32 ClearColor { get; init; }

    /// <summary>When true, outlines on <see cref="PlanarStaticPolygon"/> are drawn on the same layer.</summary>
    public bool DrawPolygonOutlines { get; init; } = true;

    /// <summary>Outline width in cells/pixels when <see cref="DrawPolygonOutlines"/> is true.</summary>
    public int OutlineThickness { get; init; } = 1;

    /// <summary>
    /// World-space bounds for <see cref="PlanarGridCoordinateSpace.WorldCells"/>.
    /// When null, bounds are derived from the camera viewport.
    /// </summary>
    public PlanarWorldBounds? WorldBounds { get; init; }

    /// <summary>Creates screen-pixel raster options for the scene camera viewport.</summary>
    public static PlanarGridRasterOptions ScreenPixels(Rgba32 clear = default) =>
        new() { Space = PlanarGridCoordinateSpace.ScreenPixels, ClearColor = clear };

    /// <summary>Creates world-cell raster options.</summary>
    /// <param name="cellSize">World units per cell.</param>
    /// <param name="worldBounds">Optional fixed bounds; when null, derived from the camera.</param>
    /// <param name="clear">Clear color written before drawing (per layer).</param>
    public static PlanarGridRasterOptions WorldCells(float cellSize, PlanarWorldBounds? worldBounds = null, Rgba32 clear = default) =>
        new()
        {
            Space = PlanarGridCoordinateSpace.WorldCells,
            CellSize = cellSize,
            WorldBounds = worldBounds,
            ClearColor = clear,
        };
}
