using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>
/// <see cref="IRayTracingBackend"/> contract shim for labs that cycle a "Vulkan" backend.
/// Rendering cannot reference Silk.NET (<c>NOV2016</c>); real Vulkan/Shaderc is
/// <c>Novolis.Silk.Compute</c>, composed only at the GAME layer. Tracing here is CPU.
/// </summary>
public sealed class VulkanRayTracingBackend : IRayTracingBackend, IDisposable
{
    private readonly CpuRayTracingBackend _cpu;

    /// <summary>Creates the CPU-backed Vulkan-labeled shim.</summary>
    /// <param name="deterministic">When true, matches <see cref="CpuRayTracingBackend"/> goldens.</param>
    public VulkanRayTracingBackend(bool deterministic = false) =>
        _cpu = new CpuRayTracingBackend(deterministic);

    /// <inheritdoc />
    public string BackendLabel => "Vulkan (CPU shim — Silk.Compute owns real VK)";

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
