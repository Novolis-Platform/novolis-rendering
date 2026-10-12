namespace Novolis.Rendering.Planar;

/// <summary>Classifies the visible parts emitted for a raised planar wall.</summary>
public enum PlanarWallPartKind
{
    /// <summary>No wall-specific classification.</summary>
    None,

    /// <summary>Vertical wall side.</summary>
    Side,

    /// <summary>Horizontal wall cap.</summary>
    Cap,
}
