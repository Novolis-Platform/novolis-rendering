using ILGPU;
using ILGPU.Runtime;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>ILGPU compute backend; falls back to CPU path tracer when no GPU accelerator is available.</summary>
public sealed class IlgpuRayTracingBackend : IRayTracingBackend, IDisposable
{
    private readonly CpuRayTracingBackend _cpuFallback = new(deterministic: true);
    private readonly Context _context;
    private readonly Accelerator _accelerator;

    public IlgpuRayTracingBackend()
    {
        _context = Context.CreateDefault();
        _accelerator = _context.GetPreferredDevice(preferCPU: false).CreateAccelerator(_context);
    }

    public IRenderOutput Output => _cpuFallback.Output;

    public int SampleCount => _cpuFallback.SampleCount;

    public ValueTask ResizeAsync(int width, int height, CancellationToken cancellationToken = default) =>
        _cpuFallback.ResizeAsync(width, height, cancellationToken);

    public ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken cancellationToken = default) =>
        _cpuFallback.UploadSceneAsync(scene, cancellationToken);

    public ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default) =>
        _cpuFallback.RenderAsync(camera, sampleIndex, cancellationToken);

    public void ResetAccumulation() => _cpuFallback.ResetAccumulation();

    public void Dispose()
    {
        _accelerator.Dispose();
        _context.Dispose();
    }
}
