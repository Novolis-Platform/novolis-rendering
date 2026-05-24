using TopologyPolygon = Novolis.Math.Topology.Polygon;

namespace Novolis.Rendering.TwoD;

/// <summary>Collision shape attached to static geometry or moving actors.</summary>
public sealed class TwoDCollider
{
    /// <summary>Creates a collider from a planar polygon.</summary>
    /// <param name="shape">Vertices in world XZ (Y ignored).</param>
    /// <param name="isTrigger">When true, blocks no movement but can be queried.</param>
    public TwoDCollider(TopologyPolygon shape, bool isTrigger = false)
    {
        Shape = shape;
        IsTrigger = isTrigger;
    }

    /// <summary>Collision polygon.</summary>
    public TopologyPolygon Shape { get; }

    /// <summary>Trigger volumes do not block movement.</summary>
    public bool IsTrigger { get; }
}
