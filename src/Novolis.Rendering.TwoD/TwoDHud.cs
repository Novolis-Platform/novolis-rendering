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

/// <summary>Base type for HUD elements.</summary>
public abstract class TwoDHudElement;

/// <summary>Bitmap-font style text (backend draws with a built-in 5x7 grid).</summary>
public sealed class TwoDHudText : TwoDHudElement
{
    /// <summary>Creates HUD text.</summary>
    public TwoDHudText(string text, float screenX, float screenY, float scale, Rgba32 color)
    {
        Text = text;
        ScreenX = screenX;
        ScreenY = screenY;
        Scale = scale;
        Color = color;
    }

    /// <summary>Display string.</summary>
    public string Text { get; set; }

    /// <summary>Horizontal pixel from left.</summary>
    public float ScreenX { get; set; }

    /// <summary>Vertical pixel from top.</summary>
    public float ScreenY { get; set; }

    /// <summary>Scale factor (pixel size of each font cell).</summary>
    public float Scale { get; set; }

    /// <summary>Text color.</summary>
    public Rgba32 Color { get; set; }
}

/// <summary>Textured HUD quad in screen pixels.</summary>
public sealed class TwoDHudSprite : TwoDHudElement
{
    /// <summary>Creates a HUD sprite quad.</summary>
    public TwoDHudSprite(
        TwoDTextureId texture,
        TwoDSourceRect source,
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
    public TwoDTextureId Texture { get; set; }

    /// <summary>UV source rect.</summary>
    public TwoDSourceRect Source { get; set; }

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
