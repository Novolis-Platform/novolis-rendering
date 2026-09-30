using System.Collections.Concurrent;
using Silk.NET.OpenGL;

namespace Novolis.Rendering.Backends.TwoD.Silk;

/// <summary>Options for <see cref="SilkTwoDFrameCaptureSession"/>.</summary>
public sealed class CaptureStreamOptions
{
    /// <summary>Capture every N frames (1 = every frame).</summary>
    public int CaptureEveryNFrames { get; init; } = 1;

    /// <summary>Maximum queued frames before dropping oldest.</summary>
    public int MaxBufferedFrames { get; init; } = 64;
}
