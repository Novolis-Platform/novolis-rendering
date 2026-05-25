namespace Novolis.Rendering.Backends.TwoD.Silk;

/// <summary>One framebuffer snapshot from Silk 2D capture.</summary>
public sealed class SilkTwoDCapturedFrame
{
    /// <summary>Monotonic frame counter since capture started.</summary>
    public required int FrameIndex { get; init; }

    /// <summary>Framebuffer width in pixels.</summary>
    public required int Width { get; init; }

    /// <summary>Framebuffer height in pixels.</summary>
    public required int Height { get; init; }

    /// <summary>PNG-encoded framebuffer bytes.</summary>
    public required byte[] Png { get; init; }

    /// <summary>Time elapsed since capture started.</summary>
    public required TimeSpan Elapsed { get; init; }
}
