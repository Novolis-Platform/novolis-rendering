namespace Novolis.Rendering.FrameGraph;

/// <summary>Base immutable resource metadata retained by a graph description.</summary>
public abstract class FrameGraphResourceDescription
{
    /// <summary>Creates resource metadata.</summary>
    protected FrameGraphResourceDescription(
        FrameGraphResourceHandle handle,
        string name,
        FrameGraphResourceLifetime lifetime,
        FrameGraphResourceUsage usage)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Handle = handle;
        Name = name;
        Lifetime = lifetime;
        Usage = usage;
    }

    /// <summary>Stable resource identity.</summary>
    public FrameGraphResourceHandle Handle { get; }

    /// <summary>Diagnostic resource name.</summary>
    public string Name { get; }

    /// <summary>Texture or buffer kind.</summary>
    public FrameGraphResourceKind Kind => Handle.Kind;

    /// <summary>Lifetime policy.</summary>
    public FrameGraphResourceLifetime Lifetime { get; }

    /// <summary>Allowed usage flags.</summary>
    public FrameGraphResourceUsage Usage { get; }

    /// <summary>Resolves a texture size, or returns no size for a buffer.</summary>
    public abstract FrameGraphSize? ResolveSize(FrameGraphSize outputSize, float qualityScale);

    /// <summary>Stable format or resource-kind label for diagnostics.</summary>
    public abstract string FormatLabel { get; }
}
