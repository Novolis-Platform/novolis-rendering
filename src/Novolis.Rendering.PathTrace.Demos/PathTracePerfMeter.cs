using Microsoft.Extensions.Logging;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>
/// Accumulates path-trace host counters and emits one <see cref="ILogger"/> line every interval (default 5s).
/// </summary>
public sealed class PathTracePerfMeter
{
    private readonly ILogger _logger;
    private readonly double _intervalSeconds;
    private double _elapsedSeconds;
    private int _frames;
    private int _blits;
    private int _busyFrames;
    private int _enqueueAttempts;
    private int _enqueueAccepted;

    /// <summary>Creates a meter that logs once every <paramref name="intervalSeconds"/>.</summary>
    public PathTracePerfMeter(ILogger logger, double intervalSeconds = 5.0)
    {
        ArgumentNullException.ThrowIfNull(logger);
        if (intervalSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalSeconds), intervalSeconds, "Interval must be positive.");
        }

        _logger = logger;
        _intervalSeconds = intervalSeconds;
    }

    /// <summary>Records whether a background enqueue started a new job.</summary>
    public void NoteEnqueue(bool accepted)
    {
        _enqueueAttempts++;
        if (accepted)
        {
            _enqueueAccepted++;
        }
    }

    /// <summary>Samples one present-loop frame; may emit a log line when the interval elapses.</summary>
    public void Tick(
        float deltaSeconds,
        float fps,
        int samples,
        bool busy,
        bool blitThisFrame,
        string backendLabel,
        int traceWidth,
        int traceHeight,
        bool orbiting = false)
    {
        if (deltaSeconds < 0)
        {
            deltaSeconds = 0;
        }

        _elapsedSeconds += deltaSeconds;
        _frames++;
        if (busy)
        {
            _busyFrames++;
        }

        if (blitThisFrame)
        {
            _blits++;
        }

        if (_elapsedSeconds >= _intervalSeconds)
        {
            Emit(fps, samples, backendLabel, traceWidth, traceHeight, orbiting);
        }
    }

    private void Emit(
        float fps,
        int samples,
        string backendLabel,
        int traceWidth,
        int traceHeight,
        bool orbiting)
    {
        var window = _elapsedSeconds;
        var frames = System.Math.Max(1, _frames);
        _logger.LogInformation(
            "path-trace perf window={Window:F1}s frames={Frames} fps={Fps:F1} (smooth={SmoothFps:F1}) samples={Samples} blitHz={BlitHz:F1} busy={BusyPct:F0}% enqueue={EnqueueOk}/{EnqueueAttempts} ({EnqueuePct:F0}%) backend={Backend} trace={Width}x{Height} orbit={Orbit}",
            window,
            _frames,
            _frames / window,
            fps,
            samples,
            _blits / window,
            100.0 * _busyFrames / frames,
            _enqueueAccepted,
            _enqueueAttempts,
            _enqueueAttempts == 0 ? 0.0 : 100.0 * _enqueueAccepted / _enqueueAttempts,
            backendLabel,
            traceWidth,
            traceHeight,
            orbiting);

        _elapsedSeconds = 0;
        _frames = 0;
        _blits = 0;
        _busyFrames = 0;
        _enqueueAttempts = 0;
        _enqueueAccepted = 0;
    }
}
