namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>Caps path-trace resolution for interactive hosts while the window may be larger.</summary>
public static class InteractiveTraceSize
{
    /// <summary>Default max interactive trace size (16:9). Keep modest — path tracing is per-pixel.</summary>
    public const int DefaultMaxWidth = 800;

    /// <summary>Default max interactive trace height.</summary>
    public const int DefaultMaxHeight = 450;

    /// <summary>
    /// Returns even width/height ≤ window and ≤ max, preserving aspect.
    /// </summary>
    public static (int Width, int Height) Clamp(
        int windowWidth,
        int windowHeight,
        int maxWidth = DefaultMaxWidth,
        int maxHeight = DefaultMaxHeight)
    {
        windowWidth = System.Math.Max(1, windowWidth);
        windowHeight = System.Math.Max(1, windowHeight);
        maxWidth = System.Math.Max(1, maxWidth);
        maxHeight = System.Math.Max(1, maxHeight);

        var scale = MathF.Min(1f, MathF.Min(maxWidth / (float)windowWidth, maxHeight / (float)windowHeight));
        var width = System.Math.Max(2, (int)(windowWidth * scale)) & ~1;
        var height = System.Math.Max(2, (int)(windowHeight * scale)) & ~1;
        return (width, height);
    }
}
