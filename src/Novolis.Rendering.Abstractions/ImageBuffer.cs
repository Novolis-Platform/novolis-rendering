using Novolis.Math.Geometry;

namespace Novolis.Rendering.Abstractions;

/// <summary>CPU-side RGBA framebuffer produced by a renderer; hosts upload or blit pixels to the GPU.</summary>
public sealed class ImageBuffer
{
    /// <summary>Allocates a zero-initialized buffer of the given dimensions.</summary>
    /// <param name="width">Pixel width; must be positive.</param>
    /// <param name="height">Pixel height; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="width"/> or <paramref name="height"/> is not positive.</exception>
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

    /// <summary>Buffer width in pixels.</summary>
    public int Width { get; }

    /// <summary>Buffer height in pixels.</summary>
    public int Height { get; }

    /// <summary>Dense row-major pixel storage.</summary>
    public Rgba32[] Pixels { get; }

    /// <summary>Writable span over <see cref="Pixels"/>.</summary>
    /// <returns>A span covering all pixels.</returns>
    public Span<Rgba32> AsSpan() => Pixels.AsSpan();

    /// <summary>Fills every pixel with <paramref name="color"/>.</summary>
    /// <param name="color">Fill color.</param>
    public void Clear(Rgba32 color)
    {
        AsSpan().Fill(color);
    }

    /// <summary>Maps (x, y) to a linear index into <see cref="Pixels"/>.</summary>
    /// <param name="x">Column index.</param>
    /// <param name="y">Row index.</param>
    /// <returns>Linear index for <c>Pixels[index]</c>.</returns>
    public int Index(int x, int y) => y * Width + x;

    /// <summary>Gets or sets the pixel at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    /// <param name="x">Column index.</param>
    /// <param name="y">Row index.</param>
    public ref Rgba32 this[int x, int y] => ref Pixels[Index(x, y)];
}
