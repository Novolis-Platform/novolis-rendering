using Novolis.Rendering.Presentation.Abstractions;

namespace Novolis.Rendering.Runtime;

/// <summary>Produces frames and owns accumulation; does not present to a window.</summary>
public interface IRayTracingBackend
{
    ValueTask ResizeAsync(int width, int height, CancellationToken cancellationToken = default);

    ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken cancellationToken = default);

    ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default);

    IRenderOutput Output { get; }

    int SampleCount { get; }

    void ResetAccumulation();
}
