namespace Novolis.Rendering.FrameGraph;

/// <summary>Positive pixel dimensions used by graph descriptions and targets.</summary>
public readonly record struct FrameGraphSize
{
    /// <summary>Creates positive dimensions.</summary>
    public FrameGraphSize(int width, int height)
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
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>Returns a positive, deterministically rounded scaled size.</summary>
    public FrameGraphSize Scale(float scale)
    {
        if (!float.IsFinite(scale) || scale <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        var width = Math.Max(1, (int)MathF.Round(Width * scale, MidpointRounding.AwayFromZero));
        var height = Math.Max(1, (int)MathF.Round(Height * scale, MidpointRounding.AwayFromZero));
        return new FrameGraphSize(width, height);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Width}x{Height}";
}
