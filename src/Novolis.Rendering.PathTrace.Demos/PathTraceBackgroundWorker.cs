using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>Runs path-tracing samples on a background thread for interactive demos.</summary>
public sealed class PathTraceBackgroundWorker : IDisposable
{
    private IRayTracingBackend _backend;
    private readonly PathTraceDisplayBuffer _display;
    private readonly object _gate = new();
    private Task? _task;
    private bool _disposed;

    /// <summary>Creates a worker bound to a backend and display buffer.</summary>
    /// <param name="backend">Ray tracing backend.</param>
    /// <param name="display">CPU display buffer updated after each job.</param>
    public PathTraceBackgroundWorker(IRayTracingBackend backend, PathTraceDisplayBuffer display)
    {
        _backend = backend;
        _display = display;
    }

    /// <summary>Points the worker at a new backend after <see cref="WaitForIdle"/>.</summary>
    /// <param name="backend">Backend to use for subsequent jobs.</param>
    public void ReplaceBackend(IRayTracingBackend backend)
    {
        WaitForIdle();
        _backend = backend;
    }

    /// <summary><see langword="true"/> when a background trace is in flight.</summary>
    public bool IsBusy
    {
        get
        {
            lock (_gate)
            {
                return _task is { IsCompleted: false };
            }
        }
    }

    /// <summary>Resets accumulation and traces <paramref name="samplesPerFrame"/> orbit samples.</summary>
    /// <param name="camera">Camera for this frame.</param>
    /// <param name="samplesPerFrame">Samples to integrate (orbit mode).</param>
    public void EnqueueOrbit(CameraSnapshot camera, int samplesPerFrame)
    {
        if (_disposed)
        {
            return;
        }

        lock (_gate)
        {
            if (_task is { IsCompleted: false })
            {
                return;
            }

            _task = Task.Run(() =>
            {
                _backend.ResetAccumulation();
                for (var s = 0; s < samplesPerFrame; s++)
                {
                    _backend.RenderAsync(camera, s).GetAwaiter().GetResult();
                }

                PublishFrame();
            });
        }
    }

    /// <summary>Enqueues a progressive accumulation batch when idle.</summary>
    /// <param name="camera">Camera for this batch.</param>
    /// <param name="sample">Current sample counter (updated on success).</param>
    /// <param name="batchSize">Samples per batch.</param>
    /// <returns><see langword="true"/> when work was queued.</returns>
    public bool TryEnqueueAccumulate(CameraSnapshot camera, ref int sample, int batchSize)
    {
        if (_disposed)
        {
            return false;
        }

        lock (_gate)
        {
            if (_task is { IsCompleted: false })
            {
                return false;
            }

            var start = sample;
            _task = Task.Run(() =>
            {
                for (var i = 0; i < batchSize; i++)
                {
                    _backend.RenderAsync(camera, start + i).GetAwaiter().GetResult();
                }

                PublishFrame();
            });
            sample = start + batchSize;
            return true;
        }
    }

    /// <summary>Blocks until the current background job completes.</summary>
    public void WaitForIdle()
    {
        Task? task;
        lock (_gate)
        {
            task = _task;
        }

        task?.GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        WaitForIdle();
    }

    private void PublishFrame()
    {
        if (!_backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h))
        {
            return;
        }

        _display.Publish(pixels, w, h, _backend.SampleCount);
    }
}
