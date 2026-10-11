namespace Novolis.Rendering.FrameGraph;

/// <summary>Identifies the attachment role of a texture.</summary>
public enum FrameGraphAttachmentKind
{
    /// <summary>Color attachment.</summary>
    Color,
    /// <summary>Depth attachment.</summary>
    Depth,
    /// <summary>Stencil attachment.</summary>
    Stencil,
    /// <summary>Resolve destination attachment.</summary>
    Resolve,
}
