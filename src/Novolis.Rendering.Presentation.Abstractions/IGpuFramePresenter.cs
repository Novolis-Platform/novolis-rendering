using Novolis.Math.Geometry;

namespace Novolis.Rendering.Presentation.Abstractions;

/// <summary>Displays a GPU-native frame handle.</summary>
public interface IGpuFramePresenter
{
    /// <summary>Presents a GPU surface to the host window.</summary>
    /// <param name="surface">GPU surface from a backend.</param>
    void PresentGpuFrame(IRenderGpuSurface surface);
}
