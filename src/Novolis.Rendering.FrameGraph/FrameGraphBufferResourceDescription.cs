namespace Novolis.Rendering.FrameGraph;

/// <summary>Buffer resource metadata with a typed handle.</summary>
public sealed class FrameGraphBufferResourceDescription : FrameGraphResourceDescription
{
    /// <summary>Creates buffer resource metadata.</summary>
    public FrameGraphBufferResourceDescription(
        FrameGraphBufferHandle handle,
        string name,
        FrameGraphBufferDescription description)
        : base(handle, name, description.Lifetime, description.Usage)
    {
        Description = description;
    }

    /// <summary>Buffer descriptor.</summary>
    public FrameGraphBufferDescription Description { get; }

    /// <summary>Buffers have no two-dimensional size.</summary>
    public override FrameGraphSize? ResolveSize(FrameGraphSize outputSize, float qualityScale) => null;

    /// <summary>Buffer diagnostic label.</summary>
    public override string FormatLabel => "Buffer";
}
