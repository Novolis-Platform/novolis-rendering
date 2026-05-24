namespace Novolis.Rendering.TwoD;

/// <summary>Texture atlas helper for grid-based sprite sheets (Mario-style strips).</summary>
public sealed class TwoDSpriteSheet
{
    /// <summary>Creates a sheet backed by a single registered texture.</summary>
    /// <param name="texture">Atlas texture id.</param>
    /// <param name="frameWidth">Frame width in pixels.</param>
    /// <param name="frameHeight">Frame height in pixels.</param>
    /// <param name="textureWidth">Atlas width.</param>
    /// <param name="textureHeight">Atlas height.</param>
    public TwoDSpriteSheet(
        TwoDTextureId texture,
        int frameWidth,
        int frameHeight,
        int textureWidth,
        int textureHeight)
    {
        Texture = texture;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        TextureWidth = textureWidth;
        TextureHeight = textureHeight;
        Columns = textureWidth / frameWidth;
        Rows = textureHeight / frameHeight;
        if (Columns <= 0 || Rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameWidth));
        }
    }

    /// <summary>Atlas texture id.</summary>
    public TwoDTextureId Texture { get; }

    /// <summary>Frame width in pixels.</summary>
    public int FrameWidth { get; }

    /// <summary>Frame height in pixels.</summary>
    public int FrameHeight { get; }

    /// <summary>Atlas width in pixels.</summary>
    public int TextureWidth { get; }

    /// <summary>Atlas height in pixels.</summary>
    public int TextureHeight { get; }

    /// <summary>Number of columns in the grid.</summary>
    public int Columns { get; }

    /// <summary>Number of rows in the grid.</summary>
    public int Rows { get; }

    /// <summary>UV rect for a frame index (row-major, left-to-right, top-to-bottom).</summary>
    /// <param name="frameIndex">Zero-based frame index.</param>
    public TwoDSourceRect GetFrame(int frameIndex)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        var col = frameIndex % Columns;
        var row = frameIndex / Columns;
        var x = col * FrameWidth;
        var y = row * FrameHeight;
        return TwoDSourceRect.FromPixels(x, y, FrameWidth, FrameHeight, TextureWidth, TextureHeight);
    }
}
