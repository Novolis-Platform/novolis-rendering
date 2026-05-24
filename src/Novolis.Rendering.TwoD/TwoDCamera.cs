using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

/// <summary>Orthographic camera mapping world XZ to screen pixels.</summary>
public sealed class TwoDCamera
{
    /// <summary>World-space center of the view (Y ignored).</summary>
    public Vector3 Position { get; set; }

    /// <summary>World units per screen pixel (zoom: smaller = more zoomed in).</summary>
    public float WorldUnitsPerPixel { get; set; } = 1f / 32f;

    /// <summary>Viewport width in pixels.</summary>
    public int ViewportWidth { get; set; } = 800;

    /// <summary>Viewport height in pixels.</summary>
    public int ViewportHeight { get; set; } = 600;

    /// <summary>Clear color for the framebuffer.</summary>
    public Rgba32 ClearColor { get; set; } = new(92, 148, 252);

    /// <summary>Builds an orthographic view-projection matrix (column-major).</summary>
    public Matrix4x4 GetViewProjectionMatrix()
    {
        var halfW = ViewportWidth * 0.5f * WorldUnitsPerPixel;
        var halfH = ViewportHeight * 0.5f * WorldUnitsPerPixel;
        var left = Position.X - halfW;
        var right = Position.X + halfW;
        var bottom = Position.Z - halfH;
        var top = Position.Z + halfH;
        return Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1f, 1f);
    }

    /// <summary>Converts screen pixels (origin top-left) to world XZ.</summary>
    /// <param name="screenX">Horizontal pixel.</param>
    /// <param name="screenY">Vertical pixel from top.</param>
    public Vector3 ScreenToWorld(float screenX, float screenY)
    {
        var halfW = ViewportWidth * 0.5f;
        var halfH = ViewportHeight * 0.5f;
        var worldX = Position.X + (screenX - halfW) * WorldUnitsPerPixel;
        var worldZ = Position.Z + (halfH - screenY) * WorldUnitsPerPixel;
        return new Vector3(worldX, 0f, worldZ);
    }

    /// <summary>Converts world XZ to screen pixels (origin top-left).</summary>
    /// <param name="world">World position.</param>
    public Vector3 WorldToScreen(Vector3 world)
    {
        var halfW = ViewportWidth * 0.5f;
        var halfH = ViewportHeight * 0.5f;
        var sx = halfW + (world.X - Position.X) / WorldUnitsPerPixel;
        var sy = halfH - (world.Z - Position.Z) / WorldUnitsPerPixel;
        return new Vector3(sx, 0f, sy);
    }
}
