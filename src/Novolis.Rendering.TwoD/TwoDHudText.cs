using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

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
