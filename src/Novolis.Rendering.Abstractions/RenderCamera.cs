using System.Numerics;

namespace Novolis.Rendering.Abstractions;

/// <summary>Observer used to cast primary rays; independent of any GPU camera type.</summary>
[Obsolete("Use Novolis.Rendering.Runtime.CameraSnapshot.")]
public readonly struct RenderCamera
{
    /// <summary>Creates a camera from an orthonormal basis and projection parameters.</summary>
    /// <param name="position">Eye position in world space.</param>
    /// <param name="forward">View direction (normalized internally).</param>
    /// <param name="up">World up hint (re-orthogonalized against forward).</param>
    /// <param name="verticalFovRadians">Vertical field of view in radians.</param>
    /// <param name="aspectRatio">Width divided by height; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="aspectRatio"/> is not positive.</exception>
    public RenderCamera(
        Vector3 position,
        Vector3 forward,
        Vector3 up,
        float verticalFovRadians,
        float aspectRatio)
    {
        if (aspectRatio <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(aspectRatio));
        }

        Position = position;
        var f = Vector3.Normalize(forward);
        Forward = f;
        var u = Vector3.Normalize(up);
        Right = Vector3.Normalize(Vector3.Cross(f, u));
        Up = Vector3.Normalize(Vector3.Cross(Right, f));
        VerticalFovRadians = verticalFovRadians;
        AspectRatio = aspectRatio;
    }

    /// <summary>Eye position in world space.</summary>
    public Vector3 Position { get; }

    /// <summary>Normalized view direction.</summary>
    public Vector3 Forward { get; }

    /// <summary>Normalized camera right axis.</summary>
    public Vector3 Right { get; }

    /// <summary>Normalized camera up axis.</summary>
    public Vector3 Up { get; }

    /// <summary>Vertical field of view in radians.</summary>
    public float VerticalFovRadians { get; }

    /// <summary>Framebuffer width divided by height.</summary>
    public float AspectRatio { get; }

    /// <summary>Builds a camera from eye, look target, and up (typical game / simulation observer).</summary>
    /// <param name="position">Eye position.</param>
    /// <param name="target">Point the camera looks at.</param>
    /// <param name="up">World up hint.</param>
    /// <param name="verticalFovDegrees">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Width divided by height.</param>
    /// <returns>A <see cref="RenderCamera"/> aimed at <paramref name="target"/>.</returns>
    public static RenderCamera LookAt(
        Vector3 position,
        Vector3 target,
        Vector3 up,
        float verticalFovDegrees,
        float aspectRatio)
    {
        var forward = target - position;
        if (forward.LengthSquared() < 1e-12f)
        {
            forward = -Vector3.UnitZ;
        }

        return new RenderCamera(
            position,
            forward,
            up,
            verticalFovDegrees * (MathF.PI / 180f),
            aspectRatio);
    }

    /// <summary>Maps from observer pose fields without referencing Simulation packages.</summary>
    /// <param name="position">Eye position.</param>
    /// <param name="target">Look-at target.</param>
    /// <param name="up">World up hint.</param>
    /// <param name="fieldOfViewDegrees">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Width divided by height.</param>
    /// <returns>A <see cref="RenderCamera"/> equivalent to <see cref="LookAt"/>.</returns>
    public static RenderCamera FromObserver(
        Vector3 position,
        Vector3 target,
        Vector3 up,
        float fieldOfViewDegrees,
        float aspectRatio) =>
        LookAt(position, target, up, fieldOfViewDegrees, aspectRatio);
}
