namespace Novolis.Rendering.FrameGraph;

/// <summary>Stable handle for a buffer resource.</summary>
public readonly record struct FrameGraphBufferHandle(int Id)
{
    /// <summary>Whether this handle has a non-negative identifier.</summary>
    public bool IsValid => Id >= 0;

    /// <summary>Converts the typed handle to the neutral resource handle.</summary>
    public static implicit operator FrameGraphResourceHandle(FrameGraphBufferHandle handle) =>
        new(FrameGraphResourceKind.Buffer, handle.Id);

    /// <inheritdoc />
    public override string ToString() => $"buffer:{Id}";
}
