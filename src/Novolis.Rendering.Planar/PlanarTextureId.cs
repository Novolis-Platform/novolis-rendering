namespace Novolis.Rendering.Planar;

/// <summary>Opaque handle to a registered texture.</summary>
/// <param name="Value">Internal id (0 = invalid).</param>
public readonly record struct PlanarTextureId(int Value)
{
    /// <summary>Invalid / empty texture id.</summary>
    public static PlanarTextureId None => new(0);

    /// <summary>Whether this id refers to a registered texture.</summary>
    public bool IsValid => Value > 0;
}
