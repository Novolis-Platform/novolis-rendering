using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Textured HUD quad in screen pixels.</summary>
public sealed class PlanarHudSprite : PlanarHudElement
{
    /// <summary>Creates a HUD sprite quad.</summary>
    public PlanarHudSprite(
        PlanarTextureId texture,
        PlanarSourceRect source,
        float screenX,
        float screenY,
        float width,
        float height)
    {
        Texture = texture;
        Source = source;
        ScreenX = screenX;
        ScreenY = screenY;
        Width = width;
        Height = height;
    }

    /// <summary>Texture id.</summary>
    public PlanarTextureId Texture { get; set; }

    /// <summary>UV source rect.</summary>
    public PlanarSourceRect Source { get; set; }

    /// <summary>Top-left X in pixels.</summary>
    public float ScreenX { get; set; }

    /// <summary>Top-left Y in pixels.</summary>
    public float ScreenY { get; set; }

    /// <summary>Draw width in pixels.</summary>
    public float Width { get; set; }

    /// <summary>Draw height in pixels.</summary>
    public float Height { get; set; }

    /// <summary>Color multiplier.</summary>
    public Rgba32 Tint { get; set; } = Rgba32.White;
}
