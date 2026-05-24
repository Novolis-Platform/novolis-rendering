using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>Vulkan compute backend placeholder; renders via embedded CPU path tracer until compute pipelines ship.</summary>
public sealed class VulkanRayTracingBackend : IRayTracingBackend
{
    private readonly CpuRayTracingBackend _inner = new(deterministic: true);

    public IRenderOutput Output => _inner.Output;

    public int SampleCount => _inner.SampleCount;

    public IRenderGpuSurface? GpuSurface { get; private set; }

    public ValueTask ResizeAsync(int width, int height, CancellationToken cancellationToken = default) =>
        _inner.ResizeAsync(width, height, cancellationToken);

    public ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken cancellationToken = default) =>
        _inner.UploadSceneAsync(scene, cancellationToken);

    public ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default)
    {
        GpuSurface = new VulkanCpuSurface(_inner.Output);
        return _inner.RenderAsync(camera, sampleIndex, cancellationToken);
    }

    public void ResetAccumulation() => _inner.ResetAccumulation();

    private sealed class VulkanCpuSurface(IRenderOutput output) : IRenderGpuSurface
    {
        public nint NativeHandle => 0;
        public int Width => output.TryGetCpuPixels(out _, out var w, out _) ? w : 0;
        public int Height => output.TryGetCpuPixels(out _, out _, out var h) ? h : 0;
    }
}
