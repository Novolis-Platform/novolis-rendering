using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Single textured quad in the world or screen space.</summary>
public sealed class PlanarSpriteInstance
{
    /// <summary>World or screen transform.</summary>
    public PlanarTransform Transform { get; set; } = new();

    /// <summary>Registered texture.</summary>
    public PlanarTextureId Texture { get; set; }

    /// <summary>UV sub-rectangle inside <see cref="Texture"/>.</summary>
    public PlanarSourceRect SourceRect { get; set; } = PlanarSourceRect.Full;

    /// <summary>Color multiplier.</summary>
    public Rgba32 Tint { get; set; } = Rgba32.White;

    /// <summary>Draw layer and sort key.</summary>
    public PlanarDrawLayer Layer { get; set; } = PlanarDrawLayer.World;

    /// <summary>Sort key within the same layer (higher draws on top).</summary>
    public int SortKey { get; set; }

    /// <summary>When true, <see cref="PlanarTransform.Position"/> is in screen pixels (origin top-left).</summary>
    public bool ScreenSpace { get; set; }
}
