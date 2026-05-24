using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

/// <summary>In-memory catalog of RGBA textures shared by sprites and HUD elements.</summary>
public sealed class TwoDTextureRegistry
{
    private readonly List<Rgba32[]> _pixelBuffers = [];
    private readonly List<(int Width, int Height, string? Name)> _metadata = [];

    /// <summary>Registers decoded RGBA pixels and returns a stable id.</summary>
    /// <param name="pixels">Row-major RGBA buffer (<c>width * height</c> elements).</param>
    /// <param name="width">Texture width.</param>
    /// <param name="height">Texture height.</param>
    /// <param name="name">Optional debug label.</param>
    /// <returns>Handle used by draw calls.</returns>
    public TwoDTextureId Register(ReadOnlySpan<Rgba32> pixels, int width, int height, string? name = null)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (pixels.Length != width * height)
        {
            throw new ArgumentException("Pixel count must equal width * height.", nameof(pixels));
        }

        _pixelBuffers.Add(pixels.ToArray());
        _metadata.Add((width, height, name));
        return new TwoDTextureId(_pixelBuffers.Count);
    }

    /// <summary>Gets metadata for a texture id.</summary>
    /// <param name="id">Registered id.</param>
    /// <exception cref="KeyNotFoundException">When <paramref name="id"/> is unknown.</exception>
    public TwoDTextureInfo GetInfo(TwoDTextureId id)
    {
        EnsureValid(id);
        var (w, h, name) = _metadata[id.Value - 1];
        return new TwoDTextureInfo(id, w, h, name);
    }

    /// <summary>Copies pixel data for upload to a GPU backend.</summary>
    /// <param name="id">Registered id.</param>
    /// <param name="pixels">Destination span (must be at least <c>width * height</c>).</param>
    /// <param name="width">Texture width.</param>
    /// <param name="height">Texture height.</param>
    public void CopyPixels(TwoDTextureId id, Span<Rgba32> pixels, out int width, out int height)
    {
        EnsureValid(id);
        var index = id.Value - 1;
        var src = _pixelBuffers[index];
        var meta = _metadata[index];
        width = meta.Width;
        height = meta.Height;
        if (pixels.Length < src.Length)
        {
            throw new ArgumentException("Destination span is too small.", nameof(pixels));
        }

        src.AsSpan().CopyTo(pixels);
    }

    /// <summary>Whether <paramref name="id"/> is registered.</summary>
    public bool Contains(TwoDTextureId id) =>
        id.IsValid && id.Value - 1 < _pixelBuffers.Count;

    private void EnsureValid(TwoDTextureId id)
    {
        if (!Contains(id))
        {
            throw new KeyNotFoundException($"Unknown texture id {id.Value}.");
        }
    }
}
