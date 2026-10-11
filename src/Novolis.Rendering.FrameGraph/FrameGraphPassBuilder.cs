namespace Novolis.Rendering.FrameGraph;

/// <summary>Collects resource accesses and policy for one graph pass.</summary>
public sealed class FrameGraphPassBuilder
{
    private readonly List<FrameGraphResourceAccess> _accesses = [];
    private readonly List<FrameGraphAttachment> _attachments = [];
    private readonly List<FrameGraphPassHandle> _dependencies = [];
    private FrameGraphTargetFeature _requiredFeatures;

    internal FrameGraphPassBuilder(FrameGraphPassHandle handle, FrameGraphPipelineStage stage)
    {
        Handle = handle;
        Stage = stage;
    }

    /// <summary>Pass identity assigned by the parent builder.</summary>
    public FrameGraphPassHandle Handle { get; }

    /// <summary>Typed renderer-owned pipeline stage.</summary>
    public FrameGraphPipelineStage Stage { get; }

    /// <summary>Compatibility view of <see cref="Stage"/>.</summary>
    public FrameGraphPassStage PassStage => (FrameGraphPassStage)Stage;

    /// <summary>Declares a sampled or read-only texture access.</summary>
    public FrameGraphPassBuilder Read(
        FrameGraphTextureHandle texture,
        FrameGraphResourceUsage usage = FrameGraphResourceUsage.Sampled)
    {
        _accesses.Add(new FrameGraphResourceAccess(texture, FrameGraphResourceAccessKind.Read, usage));
        return this;
    }

    /// <summary>Declares a read-only buffer access.</summary>
    public FrameGraphPassBuilder Read(
        FrameGraphBufferHandle buffer,
        FrameGraphResourceUsage usage = FrameGraphResourceUsage.StorageRead)
    {
        _accesses.Add(new FrameGraphResourceAccess(buffer, FrameGraphResourceAccessKind.Read, usage));
        return this;
    }

    /// <summary>Declares a write-only texture access.</summary>
    public FrameGraphPassBuilder Write(
        FrameGraphTextureHandle texture,
        FrameGraphResourceUsage usage = FrameGraphResourceUsage.StorageWrite)
    {
        _accesses.Add(new FrameGraphResourceAccess(texture, FrameGraphResourceAccessKind.Write, usage));
        return this;
    }

    /// <summary>Declares a write-only buffer access.</summary>
    public FrameGraphPassBuilder Write(
        FrameGraphBufferHandle buffer,
        FrameGraphResourceUsage usage = FrameGraphResourceUsage.StorageWrite)
    {
        _accesses.Add(new FrameGraphResourceAccess(buffer, FrameGraphResourceAccessKind.Write, usage));
        return this;
    }

    /// <summary>Declares a read/write texture access.</summary>
    public FrameGraphPassBuilder ReadWrite(
        FrameGraphTextureHandle texture,
        FrameGraphResourceUsage usage = FrameGraphResourceUsage.Storage)
    {
        _accesses.Add(new FrameGraphResourceAccess(texture, FrameGraphResourceAccessKind.ReadWrite, usage));
        return this;
    }

    /// <summary>Declares a read/write buffer access.</summary>
    public FrameGraphPassBuilder ReadWrite(
        FrameGraphBufferHandle buffer,
        FrameGraphResourceUsage usage = FrameGraphResourceUsage.Storage)
    {
        _accesses.Add(new FrameGraphResourceAccess(buffer, FrameGraphResourceAccessKind.ReadWrite, usage));
        return this;
    }

    /// <summary>Declares a color attachment write.</summary>
    public FrameGraphPassBuilder WriteColor(
        FrameGraphTextureHandle texture,
        FrameGraphLoadOperation load = FrameGraphLoadOperation.DontCare,
        FrameGraphStoreOperation store = FrameGraphStoreOperation.Store,
        FrameGraphClearValue clearValue = default) =>
        Use(
            new FrameGraphAttachment(
                texture,
                FrameGraphAttachmentKind.Color,
                FrameGraphResourceAccessKind.Write,
                load,
                store,
                clearValue));

    /// <summary>Declares a color attachment read.</summary>
    public FrameGraphPassBuilder ReadColor(FrameGraphTextureHandle texture) =>
        Use(
            new FrameGraphAttachment(
                texture,
                FrameGraphAttachmentKind.Color,
                FrameGraphResourceAccessKind.Read,
                FrameGraphLoadOperation.Load,
                FrameGraphStoreOperation.DontCare));

    /// <summary>Declares a depth attachment write.</summary>
    public FrameGraphPassBuilder WriteDepth(
        FrameGraphTextureHandle texture,
        FrameGraphLoadOperation load = FrameGraphLoadOperation.Clear,
        FrameGraphStoreOperation store = FrameGraphStoreOperation.Store,
        FrameGraphClearValue clearValue = default) =>
        Use(
            new FrameGraphAttachment(
                texture,
                FrameGraphAttachmentKind.Depth,
                FrameGraphResourceAccessKind.Write,
                load,
                store,
                clearValue));

    /// <summary>Declares a depth attachment read.</summary>
    public FrameGraphPassBuilder ReadDepth(FrameGraphTextureHandle texture) =>
        Use(
            new FrameGraphAttachment(
                texture,
                FrameGraphAttachmentKind.Depth,
                FrameGraphResourceAccessKind.Read,
                FrameGraphLoadOperation.Load,
                FrameGraphStoreOperation.DontCare));

    /// <summary>Adds a typed attachment declaration.</summary>
    public FrameGraphPassBuilder Use(FrameGraphAttachment attachment)
    {
        _attachments.Add(attachment);
        _accesses.Add(attachment.ToResourceAccess());
        return this;
    }

    /// <summary>Requires target capabilities before this pass may execute.</summary>
    public FrameGraphPassBuilder Require(FrameGraphTargetFeature features)
    {
        _requiredFeatures |= features;
        return this;
    }

    /// <summary>Adds an explicit ordering dependency on another pass.</summary>
    public FrameGraphPassBuilder DependsOn(FrameGraphPassHandle pass)
    {
        if (!_dependencies.Contains(pass))
        {
            _dependencies.Add(pass);
        }

        return this;
    }

    internal FrameGraphPassDescription Build(string name, FrameGraphPassKind kind) =>
        new(
            Handle,
            name,
            kind,
            Stage,
            FrameGraphDescription.Freeze(_accesses),
            FrameGraphDescription.Freeze(_attachments),
            FrameGraphDescription.Freeze(_dependencies),
            _requiredFeatures);
}
