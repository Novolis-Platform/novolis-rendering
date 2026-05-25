namespace Novolis.Rendering.TwoD;

/// <summary>How <see cref="TwoDLayerGridSet"/> cells map to space.</summary>
public enum TwoDGridCoordinateSpace
{
    /// <summary>Each cell is one screen pixel (origin top-left, X right, Y down).</summary>
    ScreenPixels = 0,

    /// <summary>Each cell is a world XZ slab; Y is ignored (see <see cref="TwoDGridRasterOptions.CellSize"/>).</summary>
    WorldCells = 1,
}
