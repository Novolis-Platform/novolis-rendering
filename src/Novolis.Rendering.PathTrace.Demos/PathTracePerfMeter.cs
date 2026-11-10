using Microsoft.Extensions.Logging;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>
/// Accumulates path-trace host counters and emits one <see cref="ILogger"/> line every interval (default 5s).
/// Reports blitHz as the honest present rate; host loop spin is logged separately as hostHz.
/// </summary>
public sealed class PathTracePerfMeter
{
    private readonly ILogger _logger;
    private readonly double _intervalSeconds;
    private double _elapsedSeconds;
    private int _hostFrames;
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
    /// <param name="deltaSeconds">Host frame delta.</param>
    /// <param name="blitHzSmooth">Smoothed new-frame blit rate (path-trace presents).</param>
    /// <param name="samples">Displayed sample count.</param>
    /// <param name="busy">Whether the worker is tracing.</param>
    /// <param name="blitThisFrame">True when a new path-trace buffer was presented.</param>
    /// <param name="backendLabel">Backend name for the log line.</param>
    /// <param name="traceWidth">Trace width.</param>
    /// <param name="traceHeight">Trace height.</param>
    /// <param name="orbiting">Whether the camera is moving.</param>
    public void Tick(
        float deltaSeconds,
        float blitHzSmooth,
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
        _hostFrames++;
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
            Emit(blitHzSmooth, samples, backendLabel, traceWidth, traceHeight, orbiting);
        }
    }

    private void Emit(
        float blitHzSmooth,
        int samples,
        string backendLabel,
        int traceWidth,
        int traceHeight,
        bool orbiting)
    {
        var window = _elapsedSeconds;
        var frames = System.Math.Max(1, _hostFrames);
        _logger.LogInformation(
            "path-trace perf window={Window:F1}s hostHz={HostHz:F0} blitHz={BlitHz:F1} (smooth={BlitSmooth:F1}) samples={Samples} busy={BusyPct:F0}% enqueue={EnqueueOk}/{EnqueueAttempts} ({EnqueuePct:F0}%) backend={Backend} trace={Width}x{Height} orbit={Orbit}",
            window,
            _hostFrames / window,
            _blits / window,
            blitHzSmooth,
            samples,
            100.0 * _busyFrames / frames,
            _enqueueAccepted,
            _enqueueAttempts,
            _enqueueAttempts == 0 ? 0.0 : 100.0 * _enqueueAccepted / _enqueueAttempts,
            backendLabel,
            traceWidth,
            traceHeight,
            orbiting);

        _elapsedSeconds = 0;
        _hostFrames = 0;
        _blits = 0;
        _busyFrames = 0;
        _enqueueAttempts = 0;
        _enqueueAccepted = 0;
    }
}
