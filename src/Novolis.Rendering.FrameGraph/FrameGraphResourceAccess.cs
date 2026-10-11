namespace Novolis.Rendering.FrameGraph;

/// <summary>One declared resource access in a pass.</summary>
public readonly record struct FrameGraphResourceAccess(
    FrameGraphResourceHandle Resource,
    FrameGraphResourceAccessKind Access,
    FrameGraphResourceUsage Usage)
{
    /// <summary>Creates an access for a texture.</summary>
    public FrameGraphResourceAccess(
        FrameGraphTextureHandle texture,
        FrameGraphResourceAccessKind access,
        FrameGraphResourceUsage usage)
        : this((FrameGraphResourceHandle)texture, access, usage)
    {
    }

    /// <summary>Creates an access for a buffer.</summary>
    public FrameGraphResourceAccess(
        FrameGraphBufferHandle buffer,
        FrameGraphResourceAccessKind access,
        FrameGraphResourceUsage usage)
        : this((FrameGraphResourceHandle)buffer, access, usage)
    {
    }
}
