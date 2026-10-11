namespace Novolis.Rendering.FrameGraph;

/// <summary>Typed texture attachment metadata carried by a pass.</summary>
public sealed record FrameGraphAttachment(
    FrameGraphTextureHandle Texture,
    FrameGraphAttachmentKind Kind,
    FrameGraphResourceAccessKind Access = FrameGraphResourceAccessKind.Write,
    FrameGraphLoadOperation Load = FrameGraphLoadOperation.DontCare,
    FrameGraphStoreOperation Store = FrameGraphStoreOperation.Store,
    FrameGraphClearValue ClearValue = default)
{
    /// <summary>Usage implied by the attachment role.</summary>
    public FrameGraphResourceUsage RequiredUsage =>
        Kind is FrameGraphAttachmentKind.Depth or FrameGraphAttachmentKind.Stencil
            ? FrameGraphResourceUsage.DepthStencilAttachment
            : FrameGraphResourceUsage.ColorAttachment;

    /// <summary>Converts the attachment into a resource access declaration.</summary>
    public FrameGraphResourceAccess ToResourceAccess() =>
        new(Texture, Access, RequiredUsage);
}
