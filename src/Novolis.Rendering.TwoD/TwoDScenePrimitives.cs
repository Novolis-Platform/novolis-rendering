using System.Numerics;
using Novolis.Math.Geometry;
using TopologyPolygon = Novolis.Math.Topology.Polygon;

namespace Novolis.Rendering.TwoD;

/// <summary>Helpers to build common planar polygons and sprites.</summary>
public static class TwoDScenePrimitives
{
    /// <summary>Axis-aligned rectangle on the XZ plane.</summary>
    public static TopologyPolygon Rectangle(float minX, float minZ, float maxX, float maxZ) =>
        new(
        [
            Vector3PlanarExtensions.Xz(minX, minZ),
            Vector3PlanarExtensions.Xz(maxX, minZ),
            Vector3PlanarExtensions.Xz(maxX, maxZ),
            Vector3PlanarExtensions.Xz(minX, maxZ),
        ]);

    /// <summary>Registers a sprite sheet from texture dimensions and frame size.</summary>
    public static TwoDSpriteSheet CreateSpriteSheet(
        TwoDScene scene,
        TwoDTextureId texture,
        int frameWidth,
        int frameHeight)
    {
        var info = scene.Textures.GetInfo(texture);
        return new TwoDSpriteSheet(texture, frameWidth, frameHeight, info.Width, info.Height);
    }

    /// <summary>Adds a full-screen background sprite.</summary>
    public static TwoDSpriteInstance AddBackground(
        TwoDScene scene,
        TwoDTextureId texture,
        float worldWidth,
        float worldHeight,
        int sortKey = 0)
    {
        var sprite = new TwoDSpriteInstance
        {
            Texture = texture,
            Layer = TwoDDrawLayer.Background,
            SortKey = sortKey,
            Transform =
            {
                Scale = new System.Numerics.Vector3(worldWidth, 1f, worldHeight),
            },
        };
        scene.Sprites.Add(sprite);
        return sprite;
    }
}
