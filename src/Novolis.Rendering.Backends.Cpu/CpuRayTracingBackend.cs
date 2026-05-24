using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Cpu;

/// <summary>CPU path tracing backend with progressive accumulation.</summary>
public sealed class CpuRayTracingBackend : IRayTracingBackend
{
    private readonly ImageBufferRenderOutput _output = new();
    private readonly bool _deterministic;
    private readonly object _renderLock = new();
    private ImageBuffer? _display;
    private Vector3[]? _accumulation;
    private CompiledScene _scene = CompiledScene.Empty;
    private int _sampleCount;

    public CpuRayTracingBackend(bool deterministic = false) => _deterministic = deterministic;

    public IRenderOutput Output => _output;

    public int SampleCount => _sampleCount;

    public ValueTask ResizeAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        lock (_renderLock)
        {
            _display = new ImageBuffer(width, height);
            _accumulation = new Vector3[width * height];
            Array.Clear(_accumulation);
            _output.Buffer = _display;
            _sampleCount = 0;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken cancellationToken = default)
    {
        lock (_renderLock)
        {
            _scene = scene;
            ResetAccumulationCore();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken cancellationToken = default)
    {
        lock (_renderLock)
        {
            RenderSampleCore(camera, sampleIndex);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>Renders multiple accumulation samples on the calling thread (for background workers).</summary>
    public void RenderSamples(CameraSnapshot camera, int firstSampleIndex, int count)
    {
        if (count <= 0)
        {
            return;
        }

        lock (_renderLock)
        {
            for (var i = 0; i < count; i++)
            {
                RenderSampleCore(camera, firstSampleIndex + i);
            }
        }
    }

    public void ResetAccumulation()
    {
        lock (_renderLock)
        {
            ResetAccumulationCore();
        }
    }

    private void ResetAccumulationCore()
    {
        _sampleCount = 0;
        if (_accumulation is not null)
        {
            Array.Clear(_accumulation);
        }

        _display?.Clear(Rgba32.Black);
    }

    private void RenderSampleCore(CameraSnapshot camera, int sampleIndex)
    {
        if (_display is null || _accumulation is null)
        {
            throw new InvalidOperationException("Call ResizeAsync before RenderAsync.");
        }

        PathTracerEngine.RenderSample(
            _accumulation,
            _display.Pixels,
            _display.Width,
            _display.Height,
            camera,
            _scene,
            sampleIndex,
            _deterministic);

        _sampleCount = sampleIndex + 1;
    }
}
