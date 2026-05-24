using Novolis.Math.Geometry;
using TopologyPolygon = Novolis.Math.Topology.Polygon;

namespace Novolis.Rendering.TwoD;

/// <summary>Static world polygon for platforms, pipes, and blocks.</summary>
public sealed class TwoDStaticPolygon
{
    /// <summary>Creates a static polygon with optional fill and outline.</summary>
    /// <param name="shape">World-space vertices (planar XZ).</param>
    /// <param name="fillColor">Fill color when <see cref="DrawFilled"/> is true.</param>
    public TwoDStaticPolygon(TopologyPolygon shape, Rgba32 fillColor)
    {
        Shape = shape;
        FillColor = fillColor;
    }

    /// <summary>World-space polygon.</summary>
    public TopologyPolygon Shape { get; }

    /// <summary>Fill color.</summary>
    public Rgba32 FillColor { get; set; }

    /// <summary>Outline color when <see cref="DrawOutline"/> is true.</summary>
    public Rgba32 OutlineColor { get; set; } = Rgba32.Black;

    /// <summary>Draw filled triangles.</summary>
    public bool DrawFilled { get; set; } = true;

    /// <summary>Draw edge lines.</summary>
    public bool DrawOutline { get; set; }

    /// <summary>Draw layer (usually <see cref="TwoDDrawLayer.World"/>).</summary>
    public TwoDDrawLayer Layer { get; set; } = TwoDDrawLayer.World;

    /// <summary>Sort key within the layer.</summary>
    public int SortKey { get; set; }
}
