using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>Vulkan compute path tracing backend (SPIR-V compute; no ILGPU).</summary>
public sealed class VulkanRayTracingBackend : IRayTracingBackend, IDisposable
{
    private readonly bool _deterministic;
    private readonly CpuRayTracingBackend? _cpuFallback;
    private readonly VulkanComputeDevice? _device;
    private readonly ImageBufferRenderOutput _output = new();
    private readonly object _renderLock = new();
    private ImageBuffer? _display;
    private CompiledScene _scene = CompiledScene.Empty;
    private int _sampleCount;
    private int _width;
    private int _height;
    private VulkanOutputSurface? _surface;

    public VulkanRayTracingBackend(bool deterministic = false)
    {
        _deterministic = deterministic;
        if (deterministic)
        {
            _cpuFallback = new CpuRayTracingBackend(deterministic: true);
            return;
        }

        try
        {
            var device = new VulkanComputeDevice();
            device.Initialize();
            _device = device;
        }
        catch
        {
            _cpuFallback = new CpuRayTracingBackend();
        }
    }

    public string BackendLabel => _cpuFallback is not null
        ? "Vulkan (CPU fallback)"
        : _device!.DeviceName;

    public IRenderGpuSurface? GpuSurface => _surface;

    public IRenderOutput Output => _cpuFallback?.Output ?? _output;

    public int SampleCount => _cpuFallback?.SampleCount ?? _sampleCount;

    public ValueTask ResizeAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        if (_cpuFallback is not null)
        {
            return _cpuFallback.ResizeAsync(width, height, cancellationToken);
        }

        lock (_renderLock)
        {
            _width = width;
            _height = height;
            _display = new ImageBuffer(width, height);
            _output.Buffer = _display;
            _device!.Resize(width, height);
            _sampleCount = 0;
            _device.ClearAccumulation();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken cancellationToken = default)
    {
        if (_cpuFallback is not null)
        {
            return _cpuFallback.UploadSceneAsync(scene, cancellationToken);
        }

        lock (_renderLock)
        {
            _scene = scene;
            _device!.UploadScene(scene);
            ResetAccumulationCore();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default)
    {
        if (_cpuFallback is not null)
        {
            return _cpuFallback.RenderAsync(camera, sampleIndex, cancellationToken);
        }

        lock (_renderLock)
        {
            if (_display is null)
            {
                throw new InvalidOperationException("Call ResizeAsync before RenderAsync.");
            }

            _device!.Dispatch(camera, sampleIndex, _scene.Lights.Length, _scene.BvhRootIndex);
            _device.ReadDisplayToCpu(_display.AsSpan());
            _surface = new VulkanOutputSurface(_output, _device);
            _sampleCount = sampleIndex + 1;
        }

        return ValueTask.CompletedTask;
    }

    public void ResetAccumulation()
    {
        if (_cpuFallback is not null)
        {
            _cpuFallback.ResetAccumulation();
            return;
        }

        lock (_renderLock)
        {
            ResetAccumulationCore();
        }
    }

    public void Dispose()
    {
        _device?.Dispose();
    }

    private void ResetAccumulationCore()
    {
        _sampleCount = 0;
        _display?.Clear(Rgba32.Black);
        _device?.ClearAccumulation();
    }

    private sealed class VulkanOutputSurface(IRenderOutput output, VulkanComputeDevice device) : ICpuBackedGpuSurface
    {
        public nint NativeHandle => device.DisplayBufferHandle;

        public int Width => output.TryGetCpuPixels(out _, out var w, out _) ? w : 0;

        public int Height => output.TryGetCpuPixels(out _, out _, out var h) ? h : 0;

        public bool TryGetCpuPixels(out ReadOnlySpan<Rgba32> pixels, out int width, out int height) =>
            output.TryGetCpuPixels(out pixels, out width, out height);
    }
}
