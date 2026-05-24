using Novolis.Math.Geometry;

namespace Novolis.Rendering.Abstractions;

/// <summary>CPU-side RGBA framebuffer produced by a renderer; hosts upload or blit pixels to the GPU.</summary>
public sealed class ImageBuffer
{
    public ImageBuffer(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        Pixels = new Rgba32[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public Rgba32[] Pixels { get; }

    public Span<Rgba32> AsSpan() => Pixels.AsSpan();

    public void Clear(Rgba32 color)
    {
        AsSpan().Fill(color);
    }

    public int Index(int x, int y) => y * Width + x;

    public ref Rgba32 this[int x, int y] => ref Pixels[Index(x, y)];
}
