namespace Novolis.Rendering.Planar;

/// <summary>Sprite driven by a <see cref="PlanarAnimationClip"/> (run/jump/idle loops).</summary>
public sealed class PlanarAnimatedSprite
{
    /// <summary>World transform.</summary>
    public PlanarTransform Transform { get; set; } = new();

    /// <summary>Active animation clip.</summary>
    public PlanarAnimationClip Clip { get; set; } = null!;

    /// <summary>Elapsed playback time in seconds.</summary>
    public float Time { get; set; }

    /// <summary>Draw layer.</summary>
    public PlanarDrawLayer Layer { get; set; } = PlanarDrawLayer.World;

    /// <summary>Sort key within the layer.</summary>
    public int SortKey { get; set; }

    /// <summary>Whether the clip loops.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Current local frame index (derived from <see cref="Time"/>).</summary>
    public int CurrentFrameIndex
    {
        get
        {
            if (Clip is null || Clip.FrameCount == 0)
            {
                return 0;
            }

            var frame = (int)(Time * Clip.FramesPerSecond);
            if (Loop)
            {
                frame %= Clip.FrameCount;
            }
            else
            {
                frame = System.Math.Clamp(frame, 0, Clip.FrameCount - 1);
            }

            return frame;
        }
    }

    /// <summary>Advances playback.</summary>
    /// <param name="deltaSeconds">Frame delta time.</param>
    public void Advance(float deltaSeconds) => Time += deltaSeconds;

    /// <summary>Resets playback to the first frame.</summary>
    public void Reset() => Time = 0f;
}
