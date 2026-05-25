using Novolis.Math.Arrays;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

/// <summary>In-memory per-layer grids produced from a <see cref="TwoDScene"/>.</summary>
public sealed class TwoDLayerGridSet
{
    private readonly Dictionary<TwoDDrawLayer, DenseGrid<Rgba32>> _layers = new();

    /// <summary>Grid width (pixels or world cells).</summary>
    public uint Width { get; init; }

    /// <summary>Grid height (pixels or world rows).</summary>
    public uint Height { get; init; }

    /// <summary>How cells map to space.</summary>
    public TwoDGridCoordinateSpace Space { get; init; }

    /// <summary>World cell size when <see cref="Space"/> is <see cref="TwoDGridCoordinateSpace.WorldCells"/>.</summary>
    public float CellSize { get; init; }

    /// <summary>World origin for cell (0,0) when in world-cell mode.</summary>
    public float WorldMinX { get; init; }

    /// <summary>World origin Z for cell (0,0) when in world-cell mode.</summary>
    public float WorldMinZ { get; init; }

    /// <summary>All layers that were rasterized.</summary>
    public IReadOnlyDictionary<TwoDDrawLayer, DenseGrid<Rgba32>> Layers => _layers;

    /// <summary>Layers composited in draw order (Background → Menu).</summary>
    public DenseGrid<Rgba32> Composited { get; init; } = null!;

    internal void SetLayer(TwoDDrawLayer layer, DenseGrid<Rgba32> grid) => _layers[layer] = grid;

    /// <summary>Gets a layer grid or null when that layer is empty.</summary>
    public DenseGrid<Rgba32>? TryGetLayer(TwoDDrawLayer layer) =>
        _layers.TryGetValue(layer, out var grid) ? grid : null;

    /// <summary>ASCII map of opaque cells on a layer (<c>#</c> = any alpha &gt; 0).</summary>
    /// <param name="layer">Draw layer.</param>
    /// <param name="filled">Character for non-transparent cells.</param>
    /// <param name="empty">Character for transparent cells.</param>
    public string ToAscii(TwoDDrawLayer layer, char filled = '#', char empty = '.')
    {
        if (!_layers.TryGetValue(layer, out var grid))
        {
            return string.Empty;
        }

        return FormatAscii(grid, filled, empty);
    }

    /// <summary>ASCII map of the composited frame.</summary>
    public string CompositedToAscii(char filled = '#', char empty = '.') =>
        FormatAscii(Composited, filled, empty);

    private static string FormatAscii(DenseGrid<Rgba32> grid, char filled, char empty)
    {
        var lines = new string[grid.Height];
        for (var y = 0u; y < grid.Height; y++)
        {
            var chars = new char[grid.Width];
            for (var x = 0u; x < grid.Width; x++)
            {
                var c = grid[x, y, 0] is Rgba32 cell ? cell : default;
                chars[x] = cell is { A: > 0 } ? filled : empty;
            }

            lines[y] = new string(chars);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
