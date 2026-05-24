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

/// <summary>Opaque GPU surface produced by a GPU backend; interpreted only by matching presenters.</summary>
public interface IRenderGpuSurface
{
    /// <summary>Native handle for interop (API-specific).</summary>
    nint NativeHandle { get; }

    /// <summary>Surface width in pixels.</summary>
    int Width { get; }

    /// <summary>Surface height in pixels.</summary>
    int Height { get; }
}

/// <summary>Displays a GPU-native frame handle.</summary>
public interface IGpuFramePresenter
{
    /// <summary>Presents a GPU surface to the host window.</summary>
    /// <param name="surface">GPU surface from a backend.</param>
    void PresentGpuFrame(IRenderGpuSurface surface);
}
