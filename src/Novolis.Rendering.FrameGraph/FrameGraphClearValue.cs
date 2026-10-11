using System.Numerics;

namespace Novolis.Rendering.FrameGraph;

/// <summary>Neutral clear values for color, depth, and stencil attachments.</summary>
public readonly record struct FrameGraphClearValue(Vector4 Color, float Depth, uint Stencil)
{
    /// <summary>Creates a color clear value.</summary>
    public static FrameGraphClearValue ColorClear(Vector4 color) => new(color, 1f, 0);

    /// <summary>Creates a depth and stencil clear value.</summary>
    public static FrameGraphClearValue DepthClear(float depth = 1f, uint stencil = 0) =>
        new(Vector4.Zero, depth, stencil);
}
