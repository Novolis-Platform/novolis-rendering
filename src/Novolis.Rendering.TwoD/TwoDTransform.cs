using System.Numerics;

namespace Novolis.Rendering.TwoD;

/// <summary>Planar transform using BCL <see cref="Vector3"/> (XZ world, Y unused).</summary>
public sealed class TwoDTransform
{
    /// <summary>World position; use <see cref="Vector3.X"/> and <see cref="Vector3.Z"/>.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Non-uniform scale (width on X, height on Z).</summary>
    public Vector3 Scale { get; set; } = Vector3.One;

    /// <summary>Rotation around the Y axis in radians.</summary>
    public float RotationY { get; set; }

    /// <summary>Mirror the sprite horizontally.</summary>
    public bool FlipX { get; set; }

    /// <summary>Creates a transform at the origin.</summary>
    public TwoDTransform()
    {
    }

    /// <summary>Creates a transform at <paramref name="position"/>.</summary>
    /// <param name="position">World position (Y should be 0).</param>
    public TwoDTransform(Vector3 position) => Position = position;
}
