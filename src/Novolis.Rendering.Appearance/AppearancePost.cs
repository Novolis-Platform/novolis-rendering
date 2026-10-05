using Novolis.Math.Geometry;

namespace Novolis.Rendering.Appearance;

/// <summary>Applies a post stack to a CPU framebuffer.</summary>
public static class AppearancePost
{
    /// <summary>Evaluates post ops in place.</summary>
    public static void Apply(AppearanceStack stack, Span<Rgba32> pixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(stack);
        if (width <= 0 || height <= 0 || pixels.Length < width * height)
        {
            return;
        }

        foreach (var layer in stack.Layers)
        {
            switch (layer.Op)
            {
                case AppearanceOp.Exposure:
                    Exposure(pixels, layer.Amount);
                    break;
                case AppearanceOp.Vignette:
                    Vignette(pixels, width, height, layer.Amount);
                    break;
                case AppearanceOp.ToneMap:
                    ToneMap(pixels, layer.Amount);
                    break;
                case AppearanceOp.Bloom:
                    Bloom(pixels, width, height, layer.Amount, layer.Aux, layer.Scale);
                    break;
            }
        }
    }

    private static void Exposure(Span<Rgba32> pixels, float amount)
    {
        var s = amount <= 0f ? 1f : amount;
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = AppearanceColor.Scale(pixels[i], s);
        }
    }

    private static void Vignette(Span<Rgba32> pixels, int width, int height, float amount)
    {
        var cx = width * 0.5f;
        var cy = height * 0.5f;
        var maxd = MathF.Sqrt(cx * cx + cy * cy);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var d = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / maxd;
                var m = 1f - System.Math.Clamp(amount, 0f, 1f) * d * d;
                pixels[y * width + x] = AppearanceColor.Scale(pixels[y * width + x], m);
            }
        }
    }

    private static void ToneMap(Span<Rgba32> pixels, float amount)
    {
        var mix = System.Math.Clamp(amount, 0f, 1f);
        for (var i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            var r = c.R / 255f;
            var g = c.G / 255f;
            var b = c.B / 255f;
            r = r / (1f + r);
            g = g / (1f + g);
            b = b / (1f + b);
            var mapped = new Rgba32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), c.A);
            pixels[i] = AppearanceColor.Lerp(c, mapped, mix);
        }
    }

    private static void Bloom(Span<Rgba32> pixels, int width, int height, float mix, float threshold, float radius)
    {
        mix = System.Math.Clamp(mix, 0f, 1f);
        var cut = threshold <= 0f ? 0.72f : threshold;
        var r = System.Math.Clamp((int)radius, 1, 8);
        var src = pixels.ToArray();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var accR = 0f;
                var accG = 0f;
                var accB = 0f;
                var n = 0;
                for (var oy = -r; oy <= r; oy++)
                {
                    var yy = y + oy;
                    if ((uint)yy >= (uint)height)
                    {
                        continue;
                    }

                    for (var ox = -r; ox <= r; ox++)
                    {
                        var xx = x + ox;
                        if ((uint)xx >= (uint)width)
                        {
                            continue;
                        }

                        var s = src[yy * width + xx];
                        if (AppearanceColor.Luma(s) < cut)
                        {
                            continue;
                        }

                        accR += s.R;
                        accG += s.G;
                        accB += s.B;
                        n++;
                    }
                }

                if (n == 0)
                {
                    continue;
                }

                var bloom = new Rgba32((byte)(accR / n), (byte)(accG / n), (byte)(accB / n), src[y * width + x].A);
                pixels[y * width + x] = AppearanceColor.Lerp(src[y * width + x], AppearanceColor.Add(src[y * width + x], bloom), mix);
            }
        }
    }
}
