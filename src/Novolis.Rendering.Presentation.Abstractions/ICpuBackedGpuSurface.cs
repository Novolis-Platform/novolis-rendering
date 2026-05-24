using Novolis.Math.Geometry;

namespace Novolis.Rendering.Presentation.Abstractions;

/// <summary>
/// GPU surface that is readable on the CPU (Vulkan host buffer, staging copy, etc.).
/// Presenters that cannot import the native handle can still blit via CPU pixels.
/// </summary>
public interface ICpuBackedGpuSurface : IRenderGpuSurface
{
    bool TryGetCpuPixels(out ReadOnlySpan<Rgba32> pixels, out int width, out int height);
}
