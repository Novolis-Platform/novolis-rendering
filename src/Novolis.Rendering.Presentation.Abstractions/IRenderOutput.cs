using Novolis.Math.Geometry;

namespace Novolis.Rendering.Presentation.Abstractions;

/// <summary>CPU-side render output from a ray tracing backend.</summary>
public interface IRenderOutput
{
    bool TryGetCpuPixels(out ReadOnlySpan<Rgba32> pixels, out int width, out int height);
}

/// <summary>Opaque GPU surface produced by a GPU backend; interpreted only by matching presenters.</summary>
public interface IRenderGpuSurface
{
    nint NativeHandle { get; }
    int Width { get; }
    int Height { get; }
}

/// <summary>Displays a GPU-native frame handle.</summary>
public interface IGpuFramePresenter
{
    void PresentGpuFrame(IRenderGpuSurface surface);
}
