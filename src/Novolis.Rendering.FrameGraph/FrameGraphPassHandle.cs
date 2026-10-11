namespace Novolis.Rendering.FrameGraph;

/// <summary>Stable handle for a pass declaration.</summary>
public readonly record struct FrameGraphPassHandle(int Id)
{
    /// <summary>Whether this handle has a non-negative identifier.</summary>
    public bool IsValid => Id >= 0;

    /// <inheritdoc />
    public override string ToString() => $"pass:{Id}";
}
