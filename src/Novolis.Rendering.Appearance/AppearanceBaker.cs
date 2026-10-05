using Novolis.Math.Geometry;

namespace Novolis.Rendering.Appearance;

/// <summary>Rasterizes appearance stacks to CPU pixel buffers (the 2D / blit compile target).</summary>
public static class AppearanceBaker
{
    /// <summary>Bakes a surface preview (albedo + emission) over a UV square.</summary>
    public static Rgba32[] BakeSurface(AppearanceStack stack, int width, int height, float meters, int seed, float time = 0f)
    {
        ArgumentNullException.ThrowIfNull(stack);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var pixels = new Rgba32[width * height];
        BakeSurface(stack, pixels, width, height, meters, seed, time);
        return pixels;
    }

    /// <summary>Bakes a surface preview into <paramref name="dest"/>.</summary>
    public static void BakeSurface(
        AppearanceStack stack,
        Span<Rgba32> dest,
        int width,
        int height,
        float meters,
        int seed,
        float time = 0f)
    {
        ArgumentNullException.ThrowIfNull(stack);
        if (dest.Length < width * height)
        {
            throw new ArgumentException("Destination is smaller than width * height.", nameof(dest));
        }

        var span = meters <= 0f ? 1f : meters;
        for (var y = 0; y < height; y++)
        {
            var v = y / (float)height;
            for (var x = 0; x < width; x++)
            {
                var u = x / (float)width;
                var ctx = new AppearanceContext
                {
                    Position = new System.Numerics.Vector3(u * span, 0f, v * span),
                    Normal = System.Numerics.Vector3.UnitY,
                    View = System.Numerics.Vector3.UnitY,
                    U = u * span,
                    V = v * span,
                    Time = time,
                    Seed = seed,
                };
                dest[y * width + x] = AppearanceEvaluator.PreviewColor(AppearanceEvaluator.EvaluateSurface(stack, ctx));
            }
        }
    }

    /// <summary>Bakes an effect sprite (beam / projectile) in UV 0–1.</summary>
    public static Rgba32[] BakeEffect(AppearanceStack stack, int width, int height, int seed, float time = 0f)
    {
        ArgumentNullException.ThrowIfNull(stack);
        var pixels = new Rgba32[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var ctx = new AppearanceContext
                {
                    U = (x + 0.5f) / width,
                    V = (y + 0.5f) / height,
                    Time = time,
                    Seed = seed,
                };
                pixels[y * width + x] = AppearanceEvaluator.EvaluateEffect(stack, ctx);
            }
        }

        return pixels;
    }

    /// <summary>Bakes light scattered through a volume as a 2D cone (origin at bottom-center, axis +V).</summary>
    public static Rgba32[] BakeLightVolume(
        AppearanceStack light,
        AppearanceStack volume,
        int width,
        int height,
        int seed,
        float time = 0f)
    {
        ArgumentNullException.ThrowIfNull(light);
        ArgumentNullException.ThrowIfNull(volume);
        var pixels = new Rgba32[width * height];
        var originU = 0.5f;
        var originV = 0.08f;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var u = (x + 0.5f) / width;
                var v = (y + 0.5f) / height;
                var dx = u - originU;
                var dy = v - originV;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                var axis = dy <= 1e-5f ? 0f : dy / MathF.Max(dist, 1e-5f);
                var ctx = new AppearanceContext
                {
                    U = u,
                    V = v,
                    Distance = dist * 8f,
                    AxisCosine = axis,
                    Time = time,
                    Seed = seed,
                };
                var lit = AppearanceEvaluator.EvaluateLight(light, ctx);
                var vol = AppearanceEvaluator.EvaluateVolume(volume, ctx);
                var scatter = 1f - MathF.Exp(-vol.Density * dist * 6f);
                var i = lit.Intensity * scatter;
                pixels[y * width + x] = new Rgba32(
                    (byte)System.Math.Clamp((int)(lit.Color.R * i), 0, 255),
                    (byte)System.Math.Clamp((int)(lit.Color.G * i), 0, 255),
                    (byte)System.Math.Clamp((int)(lit.Color.B * i), 0, 255),
                    (byte)System.Math.Clamp((int)(i * 220f), 0, 255));
            }
        }

        return pixels;
    }
}
