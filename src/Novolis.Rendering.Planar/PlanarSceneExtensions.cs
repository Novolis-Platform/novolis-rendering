namespace Novolis.Rendering.Planar;

/// <summary>Extension methods for <see cref="PlanarScene"/>.</summary>
public static class PlanarSceneExtensions
{
    /// <summary>Rasterizes the scene to per-layer <see cref="PlanarLayerGridSet"/> buffers for tests and debug.</summary>
    /// <param name="scene">Scene to rasterize.</param>
    /// <param name="options">Raster options.</param>
    public static PlanarLayerGridSet ToLayeredGrids(this PlanarScene scene, PlanarGridRasterOptions options) =>
        PlanarSceneGridRasterizer.Rasterize(scene, options);
}
