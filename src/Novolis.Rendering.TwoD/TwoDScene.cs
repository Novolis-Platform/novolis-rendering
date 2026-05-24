using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

/// <summary>
/// Root 2D scene: textures, sprites, animation, static geometry, collision, HUD, and menus.
/// </summary>
public sealed class TwoDScene
{
    /// <summary>Shared texture catalog.</summary>
    public TwoDTextureRegistry Textures { get; } = new();

    /// <summary>Static collision volumes.</summary>
    public TwoDCollisionWorld Collision { get; } = new();

    /// <summary>Orthographic world camera.</summary>
    public TwoDCamera Camera { get; } = new();

    /// <summary>Screen-space HUD.</summary>
    public TwoDHud Hud { get; } = new();

    /// <summary>Menu stack (title / pause / options).</summary>
    public TwoDMenuStack Menus { get; } = new();

    /// <summary>Static sprites and backgrounds.</summary>
    public List<TwoDSpriteInstance> Sprites { get; } = [];

    /// <summary>Animated characters and props.</summary>
    public List<TwoDAnimatedSprite> AnimatedSprites { get; } = [];

    /// <summary>Static filled/outline polygons (platforms, blocks).</summary>
    public List<TwoDStaticPolygon> StaticPolygons { get; } = [];

    /// <summary>Advances animated sprites.</summary>
    /// <param name="deltaSeconds">Frame delta time.</param>
    public void Update(float deltaSeconds)
    {
        foreach (var anim in AnimatedSprites)
        {
            anim.Advance(deltaSeconds);
        }
    }

    /// <summary>
    /// Adds a platform rectangle as a static polygon and matching solid collider.
    /// </summary>
    /// <param name="minX">Left world X.</param>
    /// <param name="minZ">Bottom world Z.</param>
    /// <param name="maxX">Right world X.</param>
    /// <param name="maxZ">Top world Z.</param>
    /// <param name="fillColor">Platform fill color.</param>
    public TwoDStaticPolygon AddPlatform(float minX, float minZ, float maxX, float maxZ, Rgba32 fillColor)
    {
        var poly = TwoDScenePrimitives.Rectangle(minX, minZ, maxX, maxZ);
        var visual = new TwoDStaticPolygon(poly, fillColor) { DrawFilled = true, DrawOutline = true };
        StaticPolygons.Add(visual);
        Collision.AddStatic(new TwoDCollider(poly));
        return visual;
    }
}
