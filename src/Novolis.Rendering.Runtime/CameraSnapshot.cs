using System.Numerics;

namespace Novolis.Rendering.Runtime;

/// <summary>Observer snapshot for ray generation; independent of GPU camera types.</summary>
public readonly record struct CameraSnapshot(
    Vector3 Position,
    Vector3 Forward,
    Vector3 Right,
    Vector3 Up,
    float VerticalFovRadians,
    float AspectRatio)
{
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

    public static CameraSnapshot FromObserver(
        Vector3 position,
        Vector3 target,
        Vector3 up,
        float fieldOfViewDegrees,
        float aspectRatio) =>
        LookAt(position, target, up, fieldOfViewDegrees, aspectRatio);
}
