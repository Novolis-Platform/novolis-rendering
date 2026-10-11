namespace Novolis.Rendering.FrameGraph;

/// <summary>Stable handle for a texture resource.</summary>
public readonly record struct FrameGraphTextureHandle(int Id)
{
    /// <summary>Whether this handle has a non-negative identifier.</summary>
    public bool IsValid => Id >= 0;

    /// <summary>Converts the typed handle to the neutral resource handle.</summary>
    public static implicit operator FrameGraphResourceHandle(FrameGraphTextureHandle handle) =>
        new(FrameGraphResourceKind.Texture, handle.Id);

    /// <inheritdoc />
    public override string ToString() => $"texture:{Id}";
}
