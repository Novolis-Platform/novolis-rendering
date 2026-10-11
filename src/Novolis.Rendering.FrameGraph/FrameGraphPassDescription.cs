namespace Novolis.Rendering.FrameGraph;

/// <summary>Immutable pass declaration before dependency compilation.</summary>
public sealed class FrameGraphPassDescription
{
    internal FrameGraphPassDescription(
        FrameGraphPassHandle handle,
        string name,
        FrameGraphPassKind kind,
        FrameGraphPipelineStage stage,
        IReadOnlyList<FrameGraphResourceAccess> accesses,
        IReadOnlyList<FrameGraphAttachment> attachments,
        IReadOnlyList<FrameGraphPassHandle> dependencies,
        FrameGraphTargetFeature requiredFeatures)
    {
        Handle = handle;
        Name = name;
        Kind = kind;
        Stage = stage;
        Accesses = accesses;
        Attachments = attachments;
        Dependencies = dependencies;
        RequiredFeatures = requiredFeatures;
    }

    /// <summary>Stable pass identity.</summary>
    public FrameGraphPassHandle Handle { get; }

    /// <summary>Diagnostic pass name.</summary>
    public string Name { get; }

    /// <summary>Pass category.</summary>
    public FrameGraphPassKind Kind { get; }

    /// <summary>Typed renderer-owned pipeline stage.</summary>
    public FrameGraphPipelineStage Stage { get; }

    /// <summary>Compatibility view of <see cref="Stage"/>.</summary>
    public FrameGraphPassStage PassStage => (FrameGraphPassStage)Stage;

    /// <summary>Resource accesses in declaration order.</summary>
    public IReadOnlyList<FrameGraphResourceAccess> Accesses { get; }

    /// <summary>Typed attachment declarations in declaration order.</summary>
    public IReadOnlyList<FrameGraphAttachment> Attachments { get; }

    /// <summary>Explicit dependencies requested by the author.</summary>
    public IReadOnlyList<FrameGraphPassHandle> Dependencies { get; }

    /// <summary>Target features required by this pass.</summary>
    public FrameGraphTargetFeature RequiredFeatures { get; }
}
