using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Small generated textures: solid, disc, cross, capsule.</summary>
public static class PlanarStampTextures
{
    /// <summary>1×1 solid.</summary>
    public static PlanarTextureId Solid(this PlanarTextureRegistry registry, Rgba32 color, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register([color], 1, 1, name);
    }

    /// <summary>Soft disc with power falloff.</summary>
    public static PlanarTextureId Disc(
        this PlanarTextureRegistry registry,
        int size,
        Rgba32 color,
        float falloff,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        size = int.Max(2, size);
        var px = new Rgba32[size * size];
        var c = size / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = MathF.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                if (d > 1f)
                {
                    continue;
                }

                var alpha = (byte)(color.A * MathF.Pow(1f - d, falloff));
                px[y * size + x] = new Rgba32(color.R, color.G, color.B, alpha);
            }
        }

        return registry.Register(px, size, size, name);
    }

    /// <summary>Plus-shaped spark stamp.</summary>
    public static PlanarTextureId Cross(this PlanarTextureRegistry registry, int size, Rgba32 color, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        size = int.Max(4, size);
        var px = new Rgba32[size * size];
        var c = size / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (MathF.Abs(x - c) < 1.3f || MathF.Abs(y - c) < 1.3f)
                {
                    var fade = 1f - MathF.Min(1f, (MathF.Abs(x - c) + MathF.Abs(y - c)) / 5f);
                    px[y * size + x] = new Rgba32(color.R, color.G, color.B, (byte)(color.A * fade));
                }
            }
        }

        return registry.Register(px, size, size, name);
    }

    /// <summary>Axis-aligned capsule (brass / slug silhouette).</summary>
    public static PlanarTextureId Capsule(
        this PlanarTextureRegistry registry,
        int size,
        Rgba32 color,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        size = int.Max(8, size);
        var px = new Rgba32[size * size];
        var x0 = size * 6 / 16;
        var x1 = size * 10 / 16;
        var y0 = size * 4 / 16;
        var y1 = size * 11 / 16;
        for (var y = y0; y <= y1; y++)
        {
            for (var x = x0; x <= x1; x++)
            {
                px[y * size + x] = color;
            }
        }

        return registry.Register(px, size, size, name);
    }
}
