using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>
/// Path-tracing backend. GPU Vulkan/Shaderc now lives in <c>Novolis.Silk.Compute</c>;
/// this type keeps the <see cref="IRayTracingBackend"/> contract via CPU fallback.
/// </summary>
public sealed class VulkanRayTracingBackend : IRayTracingBackend, IDisposable
{
    private readonly CpuRayTracingBackend _cpu;

    /// <summary>Creates a CPU-backed backend (Silk compute upload is composed in apps).</summary>
    public VulkanRayTracingBackend(bool deterministic = false) =>
        _cpu = new CpuRayTracingBackend(deterministic);

    /// <inheritdoc />
    public string BackendLabel => "Vulkan (CPU fallback)";

    /// <inheritdoc />
    public IRenderGpuSurface? GpuSurface => _cpu.GpuSurface;

    /// <inheritdoc />
    public IRenderOutput Output => _cpu.Output;

    /// <inheritdoc />
    public int SampleCount => _cpu.SampleCount;

    /// <inheritdoc />
    public ValueTask ResizeAsync(int width, int height, CancellationToken cancellationToken = default) =>
        _cpu.ResizeAsync(width, height, cancellationToken);

    /// <inheritdoc />
    public ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken cancellationToken = default) =>
        _cpu.UploadSceneAsync(scene, cancellationToken);

    /// <inheritdoc />
    public ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default) =>
        _cpu.RenderAsync(camera, sampleIndex, cancellationToken);

    /// <inheritdoc />
    public void ResetAccumulation() => _cpu.ResetAccumulation();

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
