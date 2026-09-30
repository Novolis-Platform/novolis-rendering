using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

/// <summary>Screen-space HUD overlay (score, lives, bars).</summary>
public sealed class TwoDHud
{
    /// <summary>HUD draw elements in paint order.</summary>
    public List<TwoDHudElement> Elements { get; } = [];

    /// <summary>Adds a text label.</summary>
    /// <param name="text">Label text.</param>
    /// <param name="screenX">Horizontal pixel from left.</param>
    /// <param name="screenY">Vertical pixel from top.</param>
    /// <param name="scale">Text scale in pixels per glyph cell.</param>
    /// <param name="color">Text color.</param>
    public TwoDHudText AddText(string text, float screenX, float screenY, float scale = 2f, Rgba32? color = null) =>
        Add(new TwoDHudText(text, screenX, screenY, scale, color ?? Rgba32.White));

    /// <summary>Adds a textured HUD icon.</summary>
    public TwoDHudSprite AddSprite(
        TwoDTextureId texture,
        TwoDSourceRect source,
        float screenX,
        float screenY,
        float width,
        float height) =>
        Add(new TwoDHudSprite(texture, source, screenX, screenY, width, height));

    private T Add<T>(T element)
        where T : TwoDHudElement
    {
        Elements.Add(element);
        return element;
    }
}
