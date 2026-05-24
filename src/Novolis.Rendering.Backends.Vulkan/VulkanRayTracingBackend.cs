using Novolis.Math.Geometry;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>
/// Vulkan presentation path: ILGPU compute tracing with a host-visible Vulkan RGBA buffer
/// exposed via <see cref="ICpuBackedGpuSurface"/>.
/// </summary>
public sealed class VulkanRayTracingBackend : IRayTracingBackend, IDisposable
{
    private readonly IlgpuRayTracingBackend _inner = new();
    private readonly VulkanHostContext _host = new();
    private VulkanOutputSurface? _surface;

    public string BackendLabel => $"Vulkan ({_inner.BackendLabel})";

    public IRenderGpuSurface? GpuSurface => _surface;

    public IRenderOutput Output => _inner.Output;

    public int SampleCount => _inner.SampleCount;

    public ValueTask ResizeAsync(int width, int height, CancellationToken cancellationToken = default) =>
        _inner.ResizeAsync(width, height, cancellationToken);

    public ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken cancellationToken = default) =>
        _inner.UploadSceneAsync(scene, cancellationToken);

    public ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default)
    {
        var task = _inner.RenderAsync(camera, sampleIndex, cancellationToken);
        if (_inner.Output.TryGetCpuPixels(out var pixels, out var w, out var h))
        {
            _host.Upload(pixels, w, h);
            _surface = new VulkanOutputSurface(_inner.Output, _host);
        }

        return task;
    }

    public void ResetAccumulation() => _inner.ResetAccumulation();

    public void Dispose()
    {
        _inner.Dispose();
        _host.Dispose();
    }

    private sealed class VulkanOutputSurface(IRenderOutput output, VulkanHostContext host) : ICpuBackedGpuSurface
    {
        public nint NativeHandle => host.BufferHandle;

        public int Width => output.TryGetCpuPixels(out _, out var w, out _) ? w : 0;

        public int Height => output.TryGetCpuPixels(out _, out _, out var h) ? h : 0;

        public bool TryGetCpuPixels(out ReadOnlySpan<Rgba32> pixels, out int width, out int height) =>
            output.TryGetCpuPixels(out pixels, out width, out height);
    }
}
