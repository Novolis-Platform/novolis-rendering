using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

/// <summary>Single textured quad in the world or screen space.</summary>
public sealed class TwoDSpriteInstance
{
    /// <summary>World or screen transform.</summary>
    public TwoDTransform Transform { get; set; } = new();

    /// <summary>Registered texture.</summary>
    public TwoDTextureId Texture { get; set; }

    /// <summary>UV sub-rectangle inside <see cref="Texture"/>.</summary>
    public TwoDSourceRect SourceRect { get; set; } = TwoDSourceRect.Full;

    /// <summary>Color multiplier.</summary>
    public Rgba32 Tint { get; set; } = Rgba32.White;

    /// <summary>Draw layer and sort key.</summary>
    public TwoDDrawLayer Layer { get; set; } = TwoDDrawLayer.World;

    /// <summary>Sort key within the same layer (higher draws on top).</summary>
    public int SortKey { get; set; }

    /// <summary>When true, <see cref="Transform.Position"/> is in screen pixels (origin top-left).</summary>
    public bool ScreenSpace { get; set; }
}
