using Novolis.Math.Geometry;

namespace Novolis.Rendering.Presentation.Abstractions;

/// <summary>CPU-side render output from a ray tracing backend.</summary>
public interface IRenderOutput
{
    /// <summary>Attempts to read the latest CPU RGBA pixels.</summary>
    /// <param name="pixels">Pixel span when successful.</param>
    /// <param name="width">Framebuffer width.</param>
    /// <param name="height">Framebuffer height.</param>
    /// <returns><see langword="true"/> when CPU pixels are available.</returns>
    bool TryGetCpuPixels(out ReadOnlySpan<Rgba32> pixels, out int width, out int height);
}
