using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Screen-space HUD overlay (score, lives, bars).</summary>
public sealed class PlanarHud
{
    /// <summary>HUD draw elements in paint order.</summary>
    public List<PlanarHudElement> Elements { get; } = [];

    /// <summary>Adds a text label.</summary>
    /// <param name="text">Label text.</param>
    /// <param name="screenX">Horizontal pixel from left.</param>
    /// <param name="screenY">Vertical pixel from top.</param>
    /// <param name="scale">Text scale in pixels per glyph cell.</param>
    /// <param name="color">Text color.</param>
    public PlanarHudText AddText(string text, float screenX, float screenY, float scale = 2f, Rgba32? color = null) =>
        Add(new PlanarHudText(text, screenX, screenY, scale, color ?? Rgba32.White));

    /// <summary>Adds a textured HUD icon.</summary>
    public PlanarHudSprite AddSprite(
        PlanarTextureId texture,
        PlanarSourceRect source,
        float screenX,
        float screenY,
        float width,
        float height) =>
        Add(new PlanarHudSprite(texture, source, screenX, screenY, width, height));

    private T Add<T>(T element)
        where T : PlanarHudElement
    {
        Elements.Add(element);
        return element;
    }
}
