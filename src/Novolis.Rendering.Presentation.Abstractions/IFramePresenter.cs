using Novolis.Math.Geometry;

namespace Novolis.Rendering.Presentation.Abstractions;

/// <summary>Displays a finished CPU frame. Implementations live in Raylib or Silk host packages.</summary>
public interface IFramePresenter
{
    void PresentCpuFrame(ReadOnlySpan<Rgba32> pixels, int width, int height);
}
