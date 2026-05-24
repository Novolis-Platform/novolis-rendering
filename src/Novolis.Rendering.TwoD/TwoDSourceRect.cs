namespace Novolis.Rendering.TwoD;

/// <summary>Normalized UV rectangle inside a texture (0–1).</summary>
/// <param name="U0">Left edge.</param>
/// <param name="V0">Top edge (OpenGL-style, V grows downward in texels).</param>
/// <param name="U1">Right edge.</param>
/// <param name="V1">Bottom edge.</param>
public readonly record struct TwoDSourceRect(float U0, float V0, float U1, float V1)
{
    /// <summary>Full texture.</summary>
    public static TwoDSourceRect Full => new(0f, 0f, 1f, 1f);

    /// <summary>Builds a rect from pixel coordinates in a texture atlas.</summary>
    /// <param name="x">Left pixel.</param>
    /// <param name="y">Top pixel.</param>
    /// <param name="width">Pixel width.</param>
    /// <param name="height">Pixel height.</param>
    /// <param name="textureWidth">Atlas width in pixels.</param>
    /// <param name="textureHeight">Atlas height in pixels.</param>
    public static TwoDSourceRect FromPixels(
        int x,
        int y,
        int width,
        int height,
        int textureWidth,
        int textureHeight)
    {
        if (textureWidth <= 0 || textureHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(textureWidth));
        }

        var u0 = x / (float)textureWidth;
        var v0 = y / (float)textureHeight;
        var u1 = (x + width) / (float)textureWidth;
        var v1 = (y + height) / (float)textureHeight;
        return new TwoDSourceRect(u0, v0, u1, v1);
    }
}
