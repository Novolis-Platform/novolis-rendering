using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;

namespace Novolis.Rendering.Presentation.Abstractions;

/// <summary>Wraps a CPU RGBA buffer as render output.</summary>
public sealed class ImageBufferRenderOutput : IRenderOutput
{
    private ImageBuffer? _buffer;

    /// <summary>Attached CPU buffer; throws when not set.</summary>
    public ImageBuffer Buffer
    {
        get => _buffer ?? throw new InvalidOperationException("No buffer attached.");
        set => _buffer = value;
    }

    /// <inheritdoc />
    public bool TryGetCpuPixels(out ReadOnlySpan<Rgba32> pixels, out int width, out int height)
    {
        if (_buffer is null)
        {
            pixels = default;
            width = 0;
            height = 0;
            return false;
        }

        pixels = _buffer.AsSpan();
        width = _buffer.Width;
        height = _buffer.Height;
        return true;
    }
}
