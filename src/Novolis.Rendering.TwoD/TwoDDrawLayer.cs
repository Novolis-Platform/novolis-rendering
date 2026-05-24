namespace Novolis.Rendering.TwoD;

/// <summary>Draw order for 2D content (lower values draw first).</summary>
public enum TwoDDrawLayer
{
    /// <summary>Full-screen or parallax background.</summary>
    Background = 0,

    /// <summary>World gameplay sprites and static geometry.</summary>
    World = 10,

    /// <summary>Foreground props in front of the player.</summary>
    Foreground = 20,

    /// <summary>Screen-space HUD (lives, score, bars).</summary>
    Hud = 100,

    /// <summary>Screen-space menus (title, pause, options).</summary>
    Menu = 200,
}
