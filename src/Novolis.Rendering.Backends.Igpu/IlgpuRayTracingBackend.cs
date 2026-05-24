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
    private readonly ImageBufferRenderOutput _output = new();
    private readonly object _renderLock = new();

    private ImageBuffer? _display;
    private Float3[]? _accumulationCpu;
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
    private Action<Index1D, int, int, int, IlgpuCameraParams, ArrayView<Float3>, ArrayView<byte>,
        ArrayView<GpuTriangle>, ArrayView<GpuMaterial>, ArrayView<GpuLight>, int, ArrayView<GpuBvhNode>, int,
        ArrayView<int>>? _traceKernel;

    public IlgpuRayTracingBackend(bool deterministic = false)
    {
        _deterministic = deterministic;
        if (deterministic)
        {
            _cpuFallback = new CpuRayTracingBackend(deterministic: true);
            return;
        }

        _context = Context.CreateDefault();
        _accelerator = _context.GetPreferredDevice(preferCPU: false).CreateAccelerator(_context);
        _useGpu = _accelerator.AcceleratorType != AcceleratorType.CPU;
        if (!_useGpu)
        {
            _cpuFallback = new CpuRayTracingBackend();
        }
    }

    public string BackendLabel => _useGpu ? $"ILGPU ({_accelerator!.Name})" : "ILGPU (CPU fallback)";

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
            _accumulationCpu = new Float3[width * height];
            Array.Clear(_accumulationCpu);
            _output.Buffer = _display;

            ReleaseGpuFrameBuffers();
            if (_useGpu)
            {
                var count = width * height;
                _accumulationGpu = _accelerator!.Allocate1D<Float3>(count);
                _displayGpu = _accelerator.Allocate1D<byte>(count * 4);
            }

            _sampleCount = 0;
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

    public ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default)
    {
        if (_cpuFallback is not null)
        {
            return _cpuFallback.RenderAsync(camera, sampleIndex, cancellationToken);
        }

        lock (_renderLock)
        {
            if (_display is null || _accumulationCpu is null)
            {
                throw new InvalidOperationException("Call ResizeAsync before RenderAsync.");
            }

            RenderSampleGpu(camera, sampleIndex);
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
        ReleaseGpuSceneBuffers();
        ReleaseGpuFrameBuffers();
        _accumulationGpu?.Dispose();
        _displayGpu?.Dispose();
        _accelerator?.Dispose();
        _context?.Dispose();
    }

    private void ResetAccumulationCore()
    {
        _sampleCount = 0;
        if (_accumulationCpu is not null)
        {
            Array.Clear(_accumulationCpu);
        }

        _display?.Clear(Rgba32.Black);
        if (_useGpu && _accumulationGpu is not null)
        {
            _accumulationGpu.MemSetToZero();
            _accelerator!.Synchronize();
        }
    }

    private void RenderSampleGpu(CameraSnapshot camera, int sampleIndex)
    {
        var accelerator = _accelerator!;
        var accumulationGpu = _accumulationGpu!;
        var cameraParams = IlgpuCameraParams.FromSnapshot(camera, _width, _height);
        accumulationGpu.CopyFromCPU(_accumulationCpu!);
        var accumulationView = accumulationGpu.View;
        var displayView = _displayGpu!.View;
        var trianglesView = _trianglesGpu!.View;
        var materialsView = _materialsGpu!.View;
        var lightsView = _lightsGpu!.View;
        var bvhView = _bvhGpu!.View;
        var orderView = _triangleOrderGpu!.View;
        var pixelCount = _width * _height;

        _traceKernel ??= accelerator.LoadAutoGroupedStreamKernel<Index1D, int, int, int, IlgpuCameraParams,
            ArrayView<Float3>, ArrayView<byte>, ArrayView<GpuTriangle>, ArrayView<GpuMaterial>, ArrayView<GpuLight>,
            int, ArrayView<GpuBvhNode>, int, ArrayView<int>>(IlgpuPathTracerKernels.TracePixelKernel);

        _traceKernel(
            pixelCount,
            _width,
            _height,
            sampleIndex,
            cameraParams,
            accumulationView,
            displayView,
            trianglesView,
            materialsView,
            lightsView,
            _scene.Lights.Length,
            bvhView,
            _bvhRootIndex,
            orderView);

        accelerator.Synchronize();
        accumulationGpu.CopyToCPU(_accumulationCpu!);
        CopyDisplayToCpu();
    }

    private void CopyDisplayToCpu()
    {
        var count = _width * _height * 4;
        var bytes = new byte[count];
        _displayGpu!.CopyToCPU(bytes);
        var pixels = _display!.Pixels;
        for (var i = 0; i < pixels.Length; i++)
        {
            var o = i * 4;
            pixels[i] = new Rgba32(bytes[o], bytes[o + 1], bytes[o + 2], bytes[o + 3]);
        }
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
}
