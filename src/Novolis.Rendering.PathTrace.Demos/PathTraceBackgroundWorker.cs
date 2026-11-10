using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>
/// Dedicated pump thread for interactive path-trace jobs (avoids thread-pool <c>Task.Run</c> scheduling jitter).
/// </summary>
public sealed class PathTraceBackgroundWorker : IDisposable
{
    private IRayTracingBackend _backend;
    private readonly PathTraceDisplayBuffer _display;
    private readonly object _gate = new();
    private readonly AutoResetEvent _wake = new(false);
    private readonly Thread _pump;
    private CancellationTokenSource _cts = new();
    private bool _resetPending;
    private bool _disposed;
    private bool _jobActive;
    private JobKind _activeKind;
    private bool _pumpRunning = true;
    private CameraSnapshot? _pendingMovingCamera;
    private int _pendingMovingSamples;
    private CameraSnapshot? _pendingAccumulateCamera;
    private int _pendingAccumulateStart;
    private int _pendingAccumulateCount;
    private bool _pendingAccumulateForceReset;
    private int _pendingAccumulateSampleAfter;

    /// <summary>Creates a worker bound to a backend and display buffer.</summary>
    public PathTraceBackgroundWorker(IRayTracingBackend backend, PathTraceDisplayBuffer display)
    {
        _backend = backend;
        _display = display;
        _pump = new Thread(PumpLoop)
        {
            IsBackground = true,
            Name = "Novolis.PathTrace.Pump",
            Priority = ThreadPriority.AboveNormal,
        };
        _pump.Start();
    }

    /// <summary>Points the worker at a new backend after <see cref="WaitForIdle"/>.</summary>
    public void ReplaceBackend(IRayTracingBackend backend)
    {
        WaitForIdle();
        lock (_gate)
        {
            _backend = backend;
            _resetPending = true;
            ClearPendingLocked();
        }
    }

    /// <summary><see langword="true"/> when a background trace is in flight.</summary>
    public bool IsBusy
    {
        get
        {
            lock (_gate)
            {
                return _jobActive;
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
            ClearPendingLocked();
            CancelLocked();
        }

        _wake.Set();
    }

    /// <summary>
    /// Marks accumulation for reset on the next job without cancelling the in-flight one.
    /// Used for auto-orbit sample buckets so poses can refine without a hard interrupt.
    /// </summary>
    public void ArmAccumulationReset()
    {
        if (_disposed)
        {
            return;
        }

        lock (_gate)
        {
            _resetPending = true;
        }
    }

    /// <summary>
    /// Interactive camera preview (orbit / drag / zoom).
    /// Coalesces to the latest pose. Never cancels an in-flight <em>moving</em> batch —
    /// cancelling every UI frame starves the GPU (preemption thrash). Only cancels accumulate.
    /// </summary>
    /// <returns><see langword="true"/> when a new background job was armed.</returns>
    public bool TryEnqueueMovingPreview(CameraSnapshot camera, int samplesPerPose = 4)
    {
        if (_disposed || samplesPerPose <= 0)
        {
            return false;
        }

        var started = false;
        lock (_gate)
        {
            _pendingMovingCamera = camera;
            _pendingMovingSamples = samplesPerPose;
            _pendingAccumulateCamera = null;

            if (_jobActive && _activeKind == JobKind.Accumulate)
            {
                // Interrupt progressive refine so motion is not stuck behind a long accumulate.
                CancelLocked();
            }
            else if (!_jobActive)
            {
                _jobActive = true;
                started = true;
            }
            // else: moving job in flight — leave it alone; pending pose is applied on chain.
        }

        _wake.Set();
        return started;
    }

    /// <summary>Alias for <see cref="TryEnqueueMovingPreview"/> (orbit hosts).</summary>
    public bool TryEnqueueOrbitPreview(CameraSnapshot camera, int samplesPerPose = 4) =>
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
            if (_jobActive)
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

            _pendingAccumulateCamera = camera;
            _pendingAccumulateStart = start;
            _pendingAccumulateCount = batchSize;
            _pendingAccumulateForceReset = forceReset;
            _pendingAccumulateSampleAfter = start + batchSize;
            sample = _pendingAccumulateSampleAfter;
            _jobActive = true;
        }

        _wake.Set();
        return true;
    }

    /// <summary>Blocks until the current background job completes (or is cancelled).</summary>
    public void WaitForIdle()
    {
        while (true)
        {
            lock (_gate)
            {
                if (!_jobActive)
                {
                    return;
                }
            }

            Thread.Sleep(0);
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
        lock (_gate)
        {
            _pumpRunning = false;
        }

        _wake.Set();
        _pump.Join(millisecondsTimeout: 5_000);
        WaitForIdle();
        _cts.Dispose();
        _wake.Dispose();
    }

    private void PumpLoop()
    {
        while (true)
        {
            Job job;
            lock (_gate)
            {
                if (!_pumpRunning && !_jobActive)
                {
                    return;
                }

                if (!TryDequeueJobLocked(out job))
                {
                    _jobActive = false;
                }
            }

            if (job.Kind == JobKind.None)
            {
                _wake.WaitOne();
                continue;
            }

            try
            {
                Execute(job);
            }
            catch (OperationCanceledException)
            {
                // Soft reset / dispose.
            }
            finally
            {
                lock (_gate)
                {
                    if (_pendingMovingCamera is not null || _pendingAccumulateCamera is not null)
                    {
                        _jobActive = true;
                    }
                    else
                    {
                        _jobActive = false;
                    }
                }
            }
        }
    }

    private bool TryDequeueJobLocked(out Job job)
    {
        if (_pendingMovingCamera is { } moving)
        {
            job = new Job(JobKind.Moving, moving, start: 0, _pendingMovingSamples, forceReset: false);
            _pendingMovingCamera = null;
            _activeKind = JobKind.Moving;
            return true;
        }

        if (_pendingAccumulateCamera is { } accumulate)
        {
            job = new Job(
                JobKind.Accumulate,
                accumulate,
                _pendingAccumulateStart,
                _pendingAccumulateCount,
                _pendingAccumulateForceReset);
            _pendingAccumulateCamera = null;
            _activeKind = JobKind.Accumulate;
            return true;
        }

        job = default;
        _activeKind = JobKind.None;
        return false;
    }

    private void Execute(Job job)
    {
        var ct = _cts.Token;
        var backend = _backend;
        ct.ThrowIfCancellationRequested();

        var reset = job.ForceReset;
        lock (_gate)
        {
            if (_resetPending)
            {
                reset = true;
                _resetPending = false;
            }
        }

        var start = job.Start;
        if (reset)
        {
            backend.ResetAccumulation();
            start = 0;
        }

        ct.ThrowIfCancellationRequested();
        ApplyTraceDepth(backend, job.Kind == JobKind.Moving ? MovingPreviewMaxDepth : FullTraceMaxDepth);
        RenderSamples(backend, job.Camera, start, job.SampleCount, ct);
        ct.ThrowIfCancellationRequested();
        PublishFrame(backend);
    }

    // Preview stays shallow; settled accumulate uses full bounce budget for cleaner GI.
    private const int MovingPreviewMaxDepth = 3;
    private const int FullTraceMaxDepth = 6;

    private static void ApplyTraceDepth(IRayTracingBackend backend, int maxDepth)
    {
        if (backend is ITraceDepthControl depth)
        {
            depth.MaxTraceDepth = maxDepth;
        }
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
            igpu.RenderBatchAsync(camera, startSampleIndex, sampleCount, flushToCpu: true, ct)
                .GetAwaiter()
                .GetResult();
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

    private void ClearPendingLocked()
    {
        _pendingMovingCamera = null;
        _pendingAccumulateCamera = null;
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

    private enum JobKind : byte
    {
        None = 0,
        Moving = 1,
        Accumulate = 2,
    }

    private readonly struct Job
    {
        public Job(JobKind kind, CameraSnapshot camera, int start, int sampleCount, bool forceReset)
        {
            Kind = kind;
            Camera = camera;
            Start = start;
            SampleCount = sampleCount;
            ForceReset = forceReset;
        }

        public JobKind Kind { get; }
        public CameraSnapshot Camera { get; }
        public int Start { get; }
        public int SampleCount { get; }
        public bool ForceReset { get; }
    }
}
