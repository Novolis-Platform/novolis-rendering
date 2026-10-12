using Novolis.Math.Geometry;
using TopologyPolygon = Novolis.Math.Topology.Polygon;

namespace Novolis.Rendering.Planar;

/// <summary>Static world polygon for platforms, pipes, and blocks.</summary>
public sealed class PlanarStaticPolygon
{
    private readonly float _minX;
    private readonly float _minZ;
    private readonly float _maxX;
    private readonly float _maxZ;

    /// <summary>Creates a static polygon with optional fill and outline.</summary>
    /// <param name="shape">World-space vertices (planar XZ).</param>
    /// <param name="fillColor">Fill color when <see cref="DrawFilled"/> is true.</param>
    public PlanarStaticPolygon(TopologyPolygon shape, Rgba32 fillColor)
    {
        ArgumentNullException.ThrowIfNull(shape);
        Shape = shape;
        FillColor = fillColor;
        if (shape.Length == 0)
        {
            return;
        }

        _minX = _maxX = shape[0].X;
        _minZ = _maxZ = shape[0].Z;
        for (var i = 1; i < shape.Length; i++)
        {
            var vertex = shape[i];
            _minX = MathF.Min(_minX, vertex.X);
            _minZ = MathF.Min(_minZ, vertex.Z);
            _maxX = MathF.Max(_maxX, vertex.X);
            _maxZ = MathF.Max(_maxZ, vertex.Z);
        }
    }

    /// <summary>World-space polygon.</summary>
    public TopologyPolygon Shape { get; }

    /// <summary>Optional prism part classification used by depth and occlusion policies.</summary>
    public PlanarWallPartKind WallPart { get; set; }

    /// <summary>Fill color.</summary>
    public Rgba32 FillColor { get; set; }

    /// <summary>Whether the polygon receives the scene light field.</summary>
    public bool ReceivesLighting { get; set; } = true;

    /// <summary>Optional albedo. World XZ / <see cref="TextureMeters"/> becomes UV.</summary>
    public PlanarTextureId Texture { get; set; }

    /// <summary>World meters mapped across one texture repeat.</summary>
    public float TextureMeters { get; set; } = 2f;

    /// <summary>Outline color when <see cref="DrawOutline"/> is true.</summary>
    public Rgba32 OutlineColor { get; set; } = Rgba32.Black;

    /// <summary>Draw filled triangles.</summary>
    public bool DrawFilled { get; set; } = true;

    /// <summary>Draw edge lines.</summary>
    public bool DrawOutline { get; set; }

    /// <summary>Draw layer (usually <see cref="PlanarDrawLayer.World"/>).</summary>
    public PlanarDrawLayer Layer { get; set; } = PlanarDrawLayer.World;

    /// <summary>Sort key within the layer.</summary>
    public int SortKey { get; set; }

    internal bool Intersects(PlanarViewport camera)
    {
        var halfW = camera.ViewportWidth * 0.5f * camera.WorldUnitsPerPixel;
        var halfH = camera.ViewportHeight * 0.5f * camera.WorldUnitsPerPixel;
        return _maxX >= camera.Position.X - halfW
            && _minX <= camera.Position.X + halfW
            && _maxZ >= camera.Position.Z - halfH
            && _minZ <= camera.Position.Z + halfH;
    }
}
