using Novolis.Rendering.Presentation.Abstractions;

namespace Novolis.Rendering.Runtime;

/// <summary>Produces frames and owns accumulation; does not present to a window.</summary>
public interface IRayTracingBackend
{
    /// <summary>Human-readable backend id for HUD and logs.</summary>
    string BackendLabel { get; }

    /// <summary>Latest GPU-native surface, when the backend produces one.</summary>
    IRenderGpuSurface? GpuSurface { get; }

    /// <summary>Resizes internal buffers for the given framebuffer dimensions.</summary>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when resize finishes.</returns>
    ValueTask ResizeAsync(int width, int height, CancellationToken cancellationToken = default);

    /// <summary>Uploads a compiled scene for subsequent <see cref="RenderAsync"/> calls.</summary>
    /// <param name="scene">Flat runtime scene.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when upload finishes.</returns>
    ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken cancellationToken = default);

    /// <summary>Traces one accumulation sample for <paramref name="camera"/>.</summary>
    /// <param name="camera">Observer snapshot.</param>
    /// <param name="sampleIndex">Zero-based sample index (progressive rendering).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the sample is integrated.</returns>
    ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default);

    /// <summary>CPU or GPU-backed output surface for presenters.</summary>
    IRenderOutput Output { get; }

    /// <summary>Number of samples accumulated since the last reset.</summary>
    int SampleCount { get; }

    /// <summary>Clears accumulation buffers and restarts progressive rendering.</summary>
    void ResetAccumulation();
}
