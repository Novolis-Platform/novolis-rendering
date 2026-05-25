using System.Collections.Concurrent;
using Silk.NET.OpenGL;

namespace Novolis.Rendering.Backends.TwoD.Silk;

/// <summary>Streaming framebuffer capture for Silk 2D games (call on the render thread after drawing).</summary>
public sealed class SilkTwoDFrameCaptureSession : IDisposable
{
    private readonly CaptureStreamOptions _options;
    private readonly ConcurrentQueue<SilkTwoDCapturedFrame> _queue = new();
    private readonly DateTime _started = DateTime.UtcNow;
    private int _frameCounter;
    private bool _disposed;

    /// <summary>Creates a capture session.</summary>
    public SilkTwoDFrameCaptureSession(CaptureStreamOptions? options = null) =>
        _options = options ?? new CaptureStreamOptions();

    /// <summary>Queue of captured frames (drain from any thread).</summary>
    public IReadOnlyCollection<SilkTwoDCapturedFrame> Pending => _queue;

    /// <summary>Attempts to dequeue one frame.</summary>
    public bool TryRead(out SilkTwoDCapturedFrame frame) => _queue.TryDequeue(out frame!);

    /// <summary>Captures the current framebuffer if the frame interval matches.</summary>
    public void CaptureAfterDraw(GL gl, int width, int height)
    {
        if (_disposed)
        {
            return;
        }

        _frameCounter++;
        if (_frameCounter % _options.CaptureEveryNFrames != 0)
        {
            return;
        }

        while (_queue.Count >= _options.MaxBufferedFrames && _queue.TryDequeue(out _))
        {
        }

        var elapsed = DateTime.UtcNow - _started;
        _queue.Enqueue(SilkTwoDFramebufferCapture.CaptureFrame(gl, width, height, _frameCounter, elapsed));
    }

    /// <inheritdoc />
    public void Dispose() => _disposed = true;
}

/// <summary>Options for <see cref="SilkTwoDFrameCaptureSession"/>.</summary>
public sealed class CaptureStreamOptions
{
    /// <summary>Capture every N frames (1 = every frame).</summary>
    public int CaptureEveryNFrames { get; init; } = 1;

    /// <summary>Maximum queued frames before dropping oldest.</summary>
    public int MaxBufferedFrames { get; init; } = 64;
}
