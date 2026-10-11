namespace Novolis.Rendering.FrameGraph;

/// <summary>Untyped resource identity used by dependency and barrier records.</summary>
public readonly record struct FrameGraphResourceHandle(FrameGraphResourceKind Kind, int Id)
{
    /// <summary>Whether this handle has a non-negative identifier.</summary>
    public bool IsValid => Id >= 0;

    /// <inheritdoc />
    public override string ToString() => $"{Kind.ToString().ToLowerInvariant()}:{Id}";
}
