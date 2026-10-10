namespace Novolis.Rendering.Planar;

/// <summary>How <see cref="PlanarLayerGridSet"/> cells map to space.</summary>
public enum PlanarGridCoordinateSpace
{
    /// <summary>Each cell is one screen pixel (origin top-left, X right, Y down).</summary>
    ScreenPixels = 0,

    /// <summary>Each cell is a world XZ slab; Y is ignored (see <see cref="PlanarGridRasterOptions.CellSize"/>).</summary>
    WorldCells = 1,
}
