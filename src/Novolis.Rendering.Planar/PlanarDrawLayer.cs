namespace Novolis.Rendering.Planar;

/// <summary>Draw order for 2D content (lower values draw first).</summary>
public enum PlanarDrawLayer
{
    /// <summary>Full-screen or parallax background.</summary>
    Background = 0,

    /// <summary>World decals, shadows, and effects that sit behind actors.</summary>
    BehindWorld = 5,

    /// <summary>World gameplay sprites and static geometry.</summary>
    World = 10,

    /// <summary>Actors and gameplay objects that should sit above world geometry.</summary>
    Actor = 20,

    /// <summary>Foreground props in front of the player.</summary>
    Foreground = 30,

    /// <summary>Bright additive-style effects such as muzzle flashes and sparks.</summary>
    Additive = 40,

    /// <summary>Screen-space HUD (lives, score, bars).</summary>
    Hud = 100,

    /// <summary>Screen-space menus (title, pause, options).</summary>
    Menu = 200,
}
