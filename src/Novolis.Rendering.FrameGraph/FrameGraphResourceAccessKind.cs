namespace Novolis.Rendering.FrameGraph;

/// <summary>Identifies the access performed by a pass.</summary>
public enum FrameGraphResourceAccessKind
{
    /// <summary>Read without mutation.</summary>
    Read,
    /// <summary>Write without reading prior contents.</summary>
    Write,
    /// <summary>Read and mutate the same resource.</summary>
    ReadWrite,
}
