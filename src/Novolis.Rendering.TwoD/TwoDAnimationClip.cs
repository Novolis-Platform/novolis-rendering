namespace Novolis.Rendering.TwoD;

/// <summary>Looping frame sequence on a <see cref="TwoDSpriteSheet"/>.</summary>
public sealed class TwoDAnimationClip
{
    /// <summary>Creates a clip from explicit frame indices.</summary>
    /// <param name="sheet">Sprite sheet.</param>
    /// <param name="frameIndices">Frame indices into the sheet grid.</param>
    /// <param name="framesPerSecond">Playback speed.</param>
    public TwoDAnimationClip(TwoDSpriteSheet sheet, int[] frameIndices, float framesPerSecond = 12f)
    {
        Sheet = sheet;
        FrameIndices = frameIndices;
        FramesPerSecond = framesPerSecond;
        if (frameIndices.Length == 0)
        {
            throw new ArgumentException("At least one frame is required.", nameof(frameIndices));
        }
    }

    /// <summary>Creates a contiguous run of frames on one row of the sheet.</summary>
    /// <param name="sheet">Sprite sheet.</param>
    /// <param name="row">Row index (0 = top).</param>
    /// <param name="startColumn">First column on the row.</param>
    /// <param name="count">Number of frames.</param>
    /// <param name="framesPerSecond">Playback speed.</param>
    public static TwoDAnimationClip FromRow(
        TwoDSpriteSheet sheet,
        int row,
        int startColumn,
        int count,
        float framesPerSecond = 12f)
    {
        var indices = new int[count];
        for (var i = 0; i < count; i++)
        {
            indices[i] = row * sheet.Columns + startColumn + i;
        }

        return new TwoDAnimationClip(sheet, indices, framesPerSecond);
    }

    /// <summary>Backing sprite sheet.</summary>
    public TwoDSpriteSheet Sheet { get; }

    /// <summary>Frame indices played in order.</summary>
    public int[] FrameIndices { get; }

    /// <summary>Playback speed in frames per second.</summary>
    public float FramesPerSecond { get; }

    /// <summary>Number of frames in the clip.</summary>
    public int FrameCount => FrameIndices.Length;

    /// <summary>UV source rect for the frame at <paramref name="localFrameIndex"/>.</summary>
    /// <param name="localFrameIndex">Index into <see cref="FrameIndices"/>.</param>
    public TwoDSourceRect GetSourceRect(int localFrameIndex)
    {
        var sheetIndex = FrameIndices[localFrameIndex % FrameIndices.Length];
        return Sheet.GetFrame(sheetIndex);
    }
}
