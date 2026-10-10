using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>
/// Root 2D scene: textures, sprites, animation, static geometry, collision, HUD, and menus.
/// </summary>
public sealed class PlanarScene
{
    /// <summary>Shared texture catalog.</summary>
    public PlanarTextureRegistry Textures { get; } = new();

    /// <summary>Static collision volumes.</summary>
    public PlanarCollisionWorld Collision { get; } = new();

    /// <summary>Orthographic world viewport.</summary>
    public PlanarViewport Camera { get; } = new();

    /// <summary>Screen-space HUD.</summary>
    public PlanarHud Hud { get; } = new();

    /// <summary>Menu stack (title / pause / options).</summary>
    public PlanarMenuStack Menus { get; } = new();

    /// <summary>Static sprites and backgrounds.</summary>
    public List<PlanarSpriteInstance> Sprites { get; } = [];

    /// <summary>Animated characters and props.</summary>
    public List<PlanarAnimatedSprite> AnimatedSprites { get; } = [];

    /// <summary>Static filled/outline polygons (platforms, blocks).</summary>
    public List<PlanarStaticPolygon> StaticPolygons { get; } = [];

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
    public PlanarStaticPolygon AddPlatform(float minX, float minZ, float maxX, float maxZ, Rgba32 fillColor)
    {
        var poly = PlanarScenePrimitives.Rectangle(minX, minZ, maxX, maxZ);
        var visual = new PlanarStaticPolygon(poly, fillColor) { DrawFilled = true, DrawOutline = true };
        StaticPolygons.Add(visual);
        Collision.AddStatic(new PlanarCollider(poly));
        return visual;
    }

    /// <summary>Tessellates this scene into a host-neutral planar draw list.</summary>
    public PlanarDrawList Tessellate(int width, int height) =>
        PlanarSceneTessellator.Tessellate(this, width, height);
}
