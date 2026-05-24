using Novolis.Math.Geometry;

namespace Novolis.Rendering.Presentation.Abstractions;

/// <summary>
/// GPU surface that is readable on the CPU (Vulkan host buffer, staging copy, etc.).
/// Presenters that cannot import the native handle can still blit via CPU pixels.
/// </summary>
public interface ICpuBackedGpuSurface : IRenderGpuSurface
{
    /// <summary>Reads CPU-accessible pixels when the backend exposes a staging copy.</summary>
    /// <param name="pixels">Pixel span when successful.</param>
    /// <param name="width">Framebuffer width.</param>
    /// <param name="height">Framebuffer height.</param>
    /// <returns><see langword="true"/> when CPU pixels are available.</returns>
    bool TryGetCpuPixels(out ReadOnlySpan<Rgba32> pixels, out int width, out int height);
}
