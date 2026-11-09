using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>Runs path-tracing samples on a background thread for interactive demos.</summary>
public sealed class PathTraceBackgroundWorker : IDisposable
{
    /// <summary>Minimum gap between moving-camera preview jobs (keeps UI/GPU from thrashing).</summary>
    private const int MovingPreviewMinIntervalMs = 40;

    private IRayTracingBackend _backend;
    private readonly PathTraceDisplayBuffer _display;
    private readonly object _gate = new();
    private Task? _task;
    private CancellationTokenSource _cts = new();
    private bool _resetPending;
    private bool _disposed;
    private CameraSnapshot? _pendingMovingCamera;
    private int _pendingMovingSamples;
    private long _nextMovingAllowedTick;

    /// <summary>Creates a worker bound to a backend and display buffer.</summary>
    public PathTraceBackgroundWorker(IRayTracingBackend backend, PathTraceDisplayBuffer display)
    {
        _backend = backend;
        _display = display;
    }

    /// <summary>Points the worker at a new backend after <see cref="WaitForIdle"/>.</summary>
    public void ReplaceBackend(IRayTracingBackend backend)
    {
        WaitForIdle();
        lock (_gate)
        {
            _backend = backend;
            _resetPending = true;
            _pendingMovingCamera = null;
        }
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

    /// <summary>
    /// Cancels any in-flight job and marks accumulation for reset on the next enqueue.
    /// Does not block the UI thread (unlike <see cref="WaitForIdle"/>).
    /// </summary>
    public void RequestReset()
    {
        if (_disposed)
        {
            return;
        }

        lock (_gate)
        {
            _resetPending = true;
            _pendingMovingCamera = null;
            CancelLocked();
        }
    }

    /// <summary>
    /// Cheap moving-camera preview (orbit / drag / zoom): coalesces to the latest pose while busy
    /// and rate-limits starts so drag does not cancel/restart every mouse delta.
    /// </summary>
    public void TryEnqueueMovingPreview(CameraSnapshot camera, int samplesPerPose = 4)
    {
        if (_disposed || samplesPerPose <= 0)
        {
            return;
        }

        lock (_gate)
        {
            _pendingMovingCamera = camera;
            _pendingMovingSamples = samplesPerPose;
            if (_task is { IsCompleted: false })
            {
                return;
            }

            if (Environment.TickCount64 < _nextMovingAllowedTick)
            {
                return;
            }

            StartMovingFromPendingLocked();
        }
    }

    /// <summary>Alias for <see cref="TryEnqueueMovingPreview"/> (orbit hosts).</summary>
    public void TryEnqueueOrbitPreview(CameraSnapshot camera, int samplesPerPose = 4) =>
        TryEnqueueMovingPreview(camera, samplesPerPose);

    /// <summary>Enqueues a progressive accumulation batch when idle.</summary>
    public bool TryEnqueueAccumulate(CameraSnapshot camera, ref int sample, int batchSize)
    {
        if (_disposed || batchSize <= 0)
        {
            return false;
        }

        lock (_gate)
        {
            if (_task is { IsCompleted: false })
            {
                return false;
            }

            _pendingMovingCamera = null;
            var start = _resetPending ? 0 : sample;
            var forceReset = _resetPending;
            if (_resetPending)
            {
                sample = 0;
            }

            var ct = _cts.Token;
            var backend = _backend;
            _task = Task.Run(() => RunJob(backend, camera, start, batchSize, forceReset, ct), ct);
            sample = start + batchSize;
            return true;
        }
    }

    /// <summary>Blocks until the current background job completes (or is cancelled).</summary>
    public void WaitForIdle()
    {
        Task? task;
        lock (_gate)
        {
            task = _task;
        }

        try
        {
            task?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Expected after RequestReset.
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        RequestReset();
        WaitForIdle();
        _cts.Dispose();
    }

    private void StartMovingFromPendingLocked()
    {
        if (_pendingMovingCamera is not { } camera)
        {
            return;
        }

        var samples = _pendingMovingSamples;
        _pendingMovingCamera = null;
        _resetPending = true;
        var ct = _cts.Token;
        var backend = _backend;
        _task = Task.Run(() => RunMovingJob(backend, camera, samples, ct), ct);
    }

    private void RunMovingJob(IRayTracingBackend backend, CameraSnapshot camera, int sampleCount, CancellationToken ct)
    {
        try
        {
            RunJob(backend, camera, startSampleIndex: 0, sampleCount, forceReset: true, ct);
        }
        catch (OperationCanceledException)
        {
            // Soft reset / dispose.
        }
        finally
        {
            lock (_gate)
            {
                _nextMovingAllowedTick = Environment.TickCount64 + MovingPreviewMinIntervalMs;
            }
        }
    }

    private void RunJob(
        IRayTracingBackend backend,
        CameraSnapshot camera,
        int startSampleIndex,
        int sampleCount,
        bool forceReset,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var reset = forceReset;
        lock (_gate)
        {
            if (_resetPending)
            {
                reset = true;
                _resetPending = false;
            }
        }

        if (reset)
        {
            backend.ResetAccumulation();
            startSampleIndex = 0;
        }

        ct.ThrowIfCancellationRequested();
        RenderSamples(backend, camera, startSampleIndex, sampleCount, ct);
        ct.ThrowIfCancellationRequested();
        PublishFrame(backend);
    }

    private static void RenderSamples(
        IRayTracingBackend backend,
        CameraSnapshot camera,
        int startSampleIndex,
        int sampleCount,
        CancellationToken ct)
    {
        if (backend is IlgpuRayTracingBackend igpu)
        {
            ct.ThrowIfCancellationRequested();
            igpu.RenderBatchAsync(camera, startSampleIndex, sampleCount, ct).GetAwaiter().GetResult();
            return;
        }

        for (var i = 0; i < sampleCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            backend.RenderAsync(camera, startSampleIndex + i, ct).GetAwaiter().GetResult();
        }
    }

    private void PublishFrame(IRayTracingBackend backend)
    {
        if (!backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h))
        {
            return;
        }

        _display.Publish(pixels, w, h, backend.SampleCount);
    }

    private void CancelLocked()
    {
        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        _cts.Dispose();
        _cts = new CancellationTokenSource();
    }
}
