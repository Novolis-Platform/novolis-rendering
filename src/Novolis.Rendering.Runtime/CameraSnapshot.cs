using System.Numerics;

namespace Novolis.Rendering.Runtime;

/// <summary>Observer snapshot for ray generation; independent of GPU camera types.</summary>
/// <param name="Position">Eye position in world space.</param>
/// <param name="Forward">Normalized view direction.</param>
/// <param name="Right">Normalized camera right axis.</param>
/// <param name="Up">Normalized camera up axis.</param>
/// <param name="VerticalFovRadians">Vertical field of view in radians.</param>
/// <param name="AspectRatio">Framebuffer width divided by height.</param>
public readonly record struct CameraSnapshot(
    Vector3 Position,
    Vector3 Forward,
    Vector3 Right,
    Vector3 Up,
    float VerticalFovRadians,
    float AspectRatio)
{
    /// <summary>Builds a snapshot from eye, look target, and up.</summary>
    /// <param name="position">Eye position.</param>
    /// <param name="target">Look-at target.</param>
    /// <param name="up">World up hint.</param>
    /// <param name="verticalFovDegrees">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Width divided by height.</param>
    /// <returns>An orthonormal <see cref="CameraSnapshot"/>.</returns>
    public static CameraSnapshot LookAt(
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

        forward = Vector3.Normalize(forward);
        var right = Vector3.Normalize(Vector3.Cross(forward, up));
        var correctedUp = Vector3.Normalize(Vector3.Cross(right, forward));
        return new CameraSnapshot(
            position,
            forward,
            right,
            correctedUp,
            verticalFovDegrees * (MathF.PI / 180f),
            aspectRatio);
    }

    /// <summary>Alias for <see cref="LookAt"/> using degrees for field of view.</summary>
    /// <param name="position">Eye position.</param>
    /// <param name="target">Look-at target.</param>
    /// <param name="up">World up hint.</param>
    /// <param name="fieldOfViewDegrees">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Width divided by height.</param>
    /// <returns>A <see cref="CameraSnapshot"/> aimed at <paramref name="target"/>.</returns>
    public static CameraSnapshot FromObserver(
        Vector3 position,
        Vector3 target,
        Vector3 up,
        float fieldOfViewDegrees,
        float aspectRatio) =>
        LookAt(position, target, up, fieldOfViewDegrees, aspectRatio);
}
