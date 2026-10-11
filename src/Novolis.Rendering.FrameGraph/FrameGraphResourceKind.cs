namespace Novolis.Rendering.FrameGraph;

/// <summary>Identifies the kind of resource referenced by a graph handle.</summary>
public enum FrameGraphResourceKind
{
    /// <summary>Two-dimensional or layered image resource.</summary>
    Texture,
    /// <summary>Structured or raw byte resource.</summary>
    Buffer,
}
