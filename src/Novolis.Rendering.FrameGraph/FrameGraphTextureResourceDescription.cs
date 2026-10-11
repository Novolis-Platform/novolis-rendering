namespace Novolis.Rendering.FrameGraph;

/// <summary>Texture resource metadata with a typed handle.</summary>
public sealed class FrameGraphTextureResourceDescription : FrameGraphResourceDescription
{
    /// <summary>Creates texture resource metadata.</summary>
    public FrameGraphTextureResourceDescription(
        FrameGraphTextureHandle handle,
        string name,
        FrameGraphTextureDescription description)
        : base(handle, name, description.Lifetime, description.Usage)
    {
        Description = description;
    }

    /// <summary>Texture descriptor.</summary>
    public FrameGraphTextureDescription Description { get; }

    /// <summary>Resolves the texture dimensions.</summary>
    public override FrameGraphSize? ResolveSize(FrameGraphSize outputSize, float qualityScale) =>
        Description.ResolveSize(outputSize, qualityScale);

    /// <summary>Texture format name.</summary>
    public override string FormatLabel => Description.Format.ToString();
}
