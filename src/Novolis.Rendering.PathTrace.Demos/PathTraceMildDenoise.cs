using Novolis.Math.Geometry;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>
/// Cheap cross bilateral for interactive low-spp frames (present-only; accumulation stays raw).
/// </summary>
public static class PathTraceMildDenoise
{
    /// <summary>Skip denoise once progressive accumulate is past this spp.</summary>
    public const int MaxSampleCount = 192;

    /// <summary>
    /// Softens Monte Carlo grain in <paramref name="pixels"/> when <paramref name="sampleCount"/> is low.
    /// No-op at high spp so sharp materials stay sharp.
    /// </summary>
    public static void ApplyInPlace(Span<Rgba32> pixels, int width, int height, int sampleCount, Span<Rgba32> scratch)
    {
        if (sampleCount <= 0 || sampleCount >= MaxSampleCount || width < 3 || height < 3)
        {
            return;
        }

        var count = width * height;
        if (pixels.Length < count || scratch.Length < count)
        {
            return;
        }

        // Blend strength falls off as spp rise (still useful early, subtle later).
        var strength = 1f - sampleCount / (float)MaxSampleCount;
        strength = strength * strength;
        pixels.CopyTo(scratch);

        for (var y = 1; y < height - 1; y++)
        {
            for (var x = 1; x < width - 1; x++)
            {
                var i = y * width + x;
                var c = scratch[i];
                var centerLum = Luma(c);

                float r = 0, g = 0, b = 0, wSum = 0;
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var n = scratch[(y + dy) * width + (x + dx)];
                        var dl = Luma(n) - centerLum;
                        var w = 1f / (1f + 40f * dl * dl);
                        if (dx == 0 && dy == 0)
                        {
                            w *= 2.5f;
                        }

                        r += n.R * w;
                        g += n.G * w;
                        b += n.B * w;
                        wSum += w;
                    }
                }

                var inv = 1f / wSum;
                var fr = (byte)System.Math.Clamp((int)(r * inv + 0.5f), 0, 255);
                var fg = (byte)System.Math.Clamp((int)(g * inv + 0.5f), 0, 255);
                var fb = (byte)System.Math.Clamp((int)(b * inv + 0.5f), 0, 255);
                pixels[i] = new Rgba32(
                    LerpByte(c.R, fr, strength),
                    LerpByte(c.G, fg, strength),
                    LerpByte(c.B, fb, strength));
            }
        }
    }

    private static float Luma(Rgba32 c) => (0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B) / 255f;

    private static byte LerpByte(byte a, byte b, float t) =>
        (byte)System.Math.Clamp((int)(a + (b - a) * t + 0.5f), 0, 255);
}
