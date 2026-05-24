using Novolis.Math.Geometry;
using Novolis.Rendering.Presentation.Abstractions;

namespace Novolis.Rendering.Presentation.Silk;

/// <summary>Headless/test presenter: forwards CPU pixels to a callback (no window).</summary>
public sealed class SilkCpuFramePresenter(Action<ReadOnlySpan<Rgba32>, int, int>? sink = null) : IFramePresenter
{
    public void PresentCpuFrame(ReadOnlySpan<Rgba32> pixels, int width, int height) =>
        sink?.Invoke(pixels, width, height);
}
