namespace Novolis.Rendering.TwoD;

/// <summary>Extension methods for <see cref="TwoDScene"/>.</summary>
public static class TwoDSceneExtensions
{
    /// <summary>Rasterizes the scene to per-layer <see cref="TwoDLayerGridSet"/> buffers for tests and debug.</summary>
    /// <param name="scene">Scene to rasterize.</param>
    /// <param name="options">Raster options.</param>
    public static TwoDLayerGridSet ToLayeredGrids(this TwoDScene scene, TwoDGridRasterOptions options) =>
        TwoDSceneGridRasterizer.Rasterize(scene, options);
}
