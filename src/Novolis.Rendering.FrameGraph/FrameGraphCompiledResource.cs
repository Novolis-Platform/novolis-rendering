namespace Novolis.Rendering.FrameGraph;

/// <summary>Resource metadata resolved for one compiled output size.</summary>
public sealed class FrameGraphCompiledResource
{
    internal FrameGraphCompiledResource(
        FrameGraphResourceDescription description,
        FrameGraphSize? resolvedSize,
        int firstUseIndex,
        int lastUseIndex)
    {
        Description = description;
        ResolvedSize = resolvedSize;
        FirstUseIndex = firstUseIndex;
        LastUseIndex = lastUseIndex;
    }

    /// <summary>Original typed resource metadata.</summary>
    public FrameGraphResourceDescription Description { get; }

    /// <summary>Resolved dimensions for textures; null for buffers.</summary>
    public FrameGraphSize? ResolvedSize { get; }

    /// <summary>First execution index that accesses the resource.</summary>
    public int FirstUseIndex { get; }

    /// <summary>Last execution index that accesses the resource.</summary>
    public int LastUseIndex { get; }

    /// <summary>Whether the resource can be considered for transient aliasing.</summary>
    public bool CanAlias =>
        Description.Lifetime == FrameGraphResourceLifetime.Transient
        && FirstUseIndex >= 0
        && LastUseIndex >= FirstUseIndex;
}
