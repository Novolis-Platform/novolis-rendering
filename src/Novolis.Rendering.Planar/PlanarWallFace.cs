using System.Numerics;

namespace Novolis.Rendering.Planar;

/// <summary>One extruded wall quad already added to a scene.</summary>
public sealed class PlanarWallFace
{
    /// <summary>Scene polygon (fill/texture/sort live here).</summary>
    public required PlanarStaticPolygon Polygon { get; init; }

    /// <summary>Footprint edge start.</summary>
    public required Vector3 A { get; init; }

    /// <summary>Footprint edge end.</summary>
    public required Vector3 B { get; init; }

    /// <summary>Loft vector used to build the quad.</summary>
    public required Vector3 Offset { get; init; }

    /// <summary>When true, an observer inside the extrusion fades the face.</summary>
    public required bool Occludes { get; init; }
}
