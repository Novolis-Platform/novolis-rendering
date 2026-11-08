using System.Numerics;
using ILGPU;
using ILGPU.Runtime;
using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>ILGPU compute path tracing backend with CPU fallback for deterministic tests.</summary>
public sealed class IlgpuRayTracingBackend : IRayTracingBackend, IDisposable
{
    private readonly bool _deterministic;
    private readonly CpuRayTracingBackend? _cpuFallback;
    private readonly Context? _context;
    private readonly Accelerator? _accelerator;
    private readonly DeferredGpuRenderOutput _deferredOutput;
    private readonly object _renderLock = new();

    private ImageBuffer? _display;
    private byte[]? _displayBytes;
    private MemoryBuffer1D<Float3, Stride1D.Dense>? _accumulationGpu;
    private MemoryBuffer1D<byte, Stride1D.Dense>? _displayGpu;
    private MemoryBuffer1D<GpuTriangle, Stride1D.Dense>? _trianglesGpu;
    private MemoryBuffer1D<GpuMaterial, Stride1D.Dense>? _materialsGpu;
    private MemoryBuffer1D<GpuLight, Stride1D.Dense>? _lightsGpu;
    private MemoryBuffer1D<GpuBvhNode, Stride1D.Dense>? _bvhGpu;
    private MemoryBuffer1D<int, Stride1D.Dense>? _triangleOrderGpu;
    private CompiledScene _scene = CompiledScene.Empty;
    private int _sampleCount;
    private int _width;
    private int _height;
    private int _bvhRootIndex = -1;
    private bool _useGpu;
    private bool _gpuDisplayDirty;
    private Action<Index1D, int, int, int, IlgpuCameraParams, ArrayView<Float3>, ArrayView<byte>,
        ArrayView<GpuTriangle>, ArrayView<GpuMaterial>, ArrayView<GpuLight>, int, ArrayView<GpuBvhNode>, int,
        ArrayView<int>>? _traceKernel;

    /// <summary>Creates an ILGPU backend; uses CPU fallback when deterministic or when no GPU is available.</summary>
    /// <param name="deterministic">When true, delegates to a deterministic CPU tracer.</param>
    public IlgpuRayTracingBackend(bool deterministic = false)
    {
        _deterministic = deterministic;
        _deferredOutput = new DeferredGpuRenderOutput(this);
        if (deterministic)
        {
            _cpuFallback = new CpuRayTracingBackend(deterministic: true);
            return;
        }

        _context = Context.CreateDefault();
        var device = IlgpuDeviceSelector.SelectDevice(_context);
        _accelerator = device.CreateAccelerator(_context);
        _useGpu = _accelerator.AcceleratorType != AcceleratorType.CPU;
        if (_useGpu && !TryLoadTraceKernel())
        {
            // Some CUDA/OpenCL ILGPU compilers ICE on this kernel; keep CPU path green.
            _useGpu = false;
        }

        if (!_useGpu)
        {
            _cpuFallback = new CpuRayTracingBackend();
        }
    }

    /// <inheritdoc />
    public string BackendLabel => _useGpu ? $"ILGPU ({_accelerator!.Name})" : "ILGPU (CPU fallback)";

    /// <inheritdoc />
    public IRenderGpuSurface? GpuSurface => null;

    /// <inheritdoc />
    public IRenderOutput Output => _cpuFallback?.Output ?? _deferredOutput;

    /// <inheritdoc />
    public int SampleCount => _cpuFallback?.SampleCount ?? _sampleCount;

    /// <inheritdoc />
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
            _displayBytes = new byte[width * height * 4];
            _gpuDisplayDirty = false;

            ReleaseGpuFrameBuffers();
            if (_useGpu)
            {
                var count = width * height;
                _accumulationGpu = _accelerator!.Allocate1D<Float3>(count);
                _displayGpu = _accelerator.Allocate1D<byte>(count * 4);
                _accumulationGpu.MemSetToZero();
            }

            _sampleCount = 0;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken cancellationToken = default)
    {
        if (_cpuFallback is not null)
        {
            return _cpuFallback.UploadSceneAsync(scene, cancellationToken);
        }

        lock (_renderLock)
        {
            _scene = scene;
            _bvhRootIndex = scene.BvhRootIndex;
            ReleaseGpuSceneBuffers();
            if (_useGpu)
            {
                UploadSceneToGpu(scene);
            }

            ResetAccumulationCore();
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default)
    {
        if (_cpuFallback is not null)
        {
            return _cpuFallback.RenderAsync(camera, sampleIndex, cancellationToken);
        }

        lock (_renderLock)
        {
            if (_display is null || _accumulationGpu is null || _displayGpu is null)
            {
                throw new InvalidOperationException("Call ResizeAsync before RenderAsync.");
            }

            // Keep accumulation resident on the GPU. Do not sync/download until Output is read.
            EnqueueTraceKernel(camera, sampleIndex);
            _sampleCount = sampleIndex + 1;
            _gpuDisplayDirty = true;
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Queues <paramref name="sampleCount"/> samples then flushes the display once.
    /// Prefer this over repeated <see cref="RenderAsync"/> when publishing a frame after a batch.
    /// </summary>
    public ValueTask RenderBatchAsync(
        CameraSnapshot camera,
        int startSampleIndex,
        int sampleCount,
        CancellationToken cancellationToken = default)
    {
        if (_cpuFallback is not null)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                _cpuFallback.RenderAsync(camera, startSampleIndex + i, cancellationToken).GetAwaiter().GetResult();
            }

            return ValueTask.CompletedTask;
        }

        if (sampleCount <= 0)
        {
            return ValueTask.CompletedTask;
        }

        lock (_renderLock)
        {
            if (_display is null || _accumulationGpu is null || _displayGpu is null)
            {
                throw new InvalidOperationException("Call ResizeAsync before RenderBatchAsync.");
            }

            for (var i = 0; i < sampleCount; i++)
            {
                EnqueueTraceKernel(camera, startSampleIndex + i);
            }

            _sampleCount = startSampleIndex + sampleCount;
            _gpuDisplayDirty = true;
            FlushDisplayToCpuUnlocked();
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
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

    /// <summary>Releases ILGPU device and GPU buffers.</summary>
    public void Dispose()
    {
        ReleaseGpuSceneBuffers();
        ReleaseGpuFrameBuffers();
        _accumulationGpu?.Dispose();
        _displayGpu?.Dispose();
        _accelerator?.Dispose();
        _context?.Dispose();
    }

    private void EnsureCpuDisplay()
    {
        if (_cpuFallback is not null)
        {
            return;
        }

        lock (_renderLock)
        {
            FlushDisplayToCpuUnlocked();
        }
    }

    private bool TryGetCpuPixels(out ReadOnlySpan<Rgba32> pixels, out int width, out int height)
    {
        EnsureCpuDisplay();
        if (_display is null)
        {
            pixels = default;
            width = 0;
            height = 0;
            return false;
        }

        pixels = _display.AsSpan();
        width = _display.Width;
        height = _display.Height;
        return true;
    }

    private bool TryLoadTraceKernel()
    {
        try
        {
            _traceKernel = _accelerator!.LoadAutoGroupedStreamKernel<Index1D, int, int, int, IlgpuCameraParams,
                ArrayView<Float3>, ArrayView<byte>, ArrayView<GpuTriangle>, ArrayView<GpuMaterial>, ArrayView<GpuLight>,
                int, ArrayView<GpuBvhNode>, int, ArrayView<int>>(IlgpuPathTracerKernels.TracePixelKernel);
            return true;
        }
        catch (Exception ex) when (IsIlgpuCompilerFailure(ex))
        {
            _traceKernel = null;
            return false;
        }
    }

    private static bool IsIlgpuCompilerFailure(Exception ex) =>
        ex.GetType().Name.Contains("InternalCompiler", StringComparison.Ordinal)
        || ex.Message.Contains("internal compiler error", StringComparison.OrdinalIgnoreCase);

    private void ResetAccumulationCore()
    {
        _sampleCount = 0;
        _display?.Clear(Rgba32.Black);
        _gpuDisplayDirty = false;
        if (_useGpu && _accumulationGpu is not null)
        {
            _accumulationGpu.MemSetToZero();
            if (_displayGpu is not null)
            {
                _displayGpu.MemSetToZero();
            }

            _accelerator!.Synchronize();
        }
    }

    private void EnqueueTraceKernel(CameraSnapshot camera, int sampleIndex)
    {
        var kernel = _traceKernel
            ?? throw new InvalidOperationException("GPU kernel was not loaded; backend should have fallen back to CPU.");
        var cameraParams = IlgpuCameraParams.FromSnapshot(camera, _width, _height);
        var pixelCount = _width * _height;
        kernel(
            pixelCount,
            _width,
            _height,
            sampleIndex,
            cameraParams,
            _accumulationGpu!.View,
            _displayGpu!.View,
            _trianglesGpu!.View,
            _materialsGpu!.View,
            _lightsGpu!.View,
            _scene.Lights.Length,
            _bvhGpu!.View,
            _bvhRootIndex,
            _triangleOrderGpu!.View);
    }

    private void FlushDisplayToCpuUnlocked()
    {
        if (!_gpuDisplayDirty || _display is null || _displayGpu is null || _displayBytes is null)
        {
            return;
        }

        _accelerator!.Synchronize();
        _displayGpu.CopyToCPU(_displayBytes);
        var pixels = _display.Pixels;
        var bytes = _displayBytes;
        for (var i = 0; i < pixels.Length; i++)
        {
            var o = i * 4;
            pixels[i] = new Rgba32(bytes[o], bytes[o + 1], bytes[o + 2], bytes[o + 3]);
        }

        _gpuDisplayDirty = false;
    }

    private void UploadSceneToGpu(CompiledScene scene)
    {
        var accelerator = _accelerator!;
        _trianglesGpu = accelerator.Allocate1D(scene.Triangles.ToArray());
        _materialsGpu = accelerator.Allocate1D(scene.Materials.ToArray());
        _lightsGpu = scene.Lights.IsEmpty
            ? accelerator.Allocate1D<GpuLight>(1)
            : accelerator.Allocate1D(scene.Lights.ToArray());
        _bvhGpu = scene.BvhNodes.IsEmpty
            ? accelerator.Allocate1D<GpuBvhNode>(1)
            : accelerator.Allocate1D(scene.BvhNodes.Select(GpuBvhNode.From).ToArray());
        _triangleOrderGpu = scene.TriangleOrder.IsEmpty
            ? accelerator.Allocate1D<int>(1)
            : accelerator.Allocate1D(scene.TriangleOrder.ToArray());
    }

    private void ReleaseGpuFrameBuffers()
    {
        _accumulationGpu?.Dispose();
        _displayGpu?.Dispose();
        _accumulationGpu = null;
        _displayGpu = null;
    }

    private void ReleaseGpuSceneBuffers()
    {
        _trianglesGpu?.Dispose();
        _materialsGpu?.Dispose();
        _lightsGpu?.Dispose();
        _bvhGpu?.Dispose();
        _triangleOrderGpu?.Dispose();
        _trianglesGpu = null;
        _materialsGpu = null;
        _lightsGpu = null;
        _bvhGpu = null;
        _triangleOrderGpu = null;
    }

    private sealed class DeferredGpuRenderOutput(IlgpuRayTracingBackend owner) : IRenderOutput
    {
        public bool TryGetCpuPixels(out ReadOnlySpan<Rgba32> pixels, out int width, out int height) =>
            owner.TryGetCpuPixels(out pixels, out width, out height);
    }
}
