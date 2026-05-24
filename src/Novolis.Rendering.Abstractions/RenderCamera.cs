using System.Numerics;

namespace Novolis.Rendering.Abstractions;

/// <summary>Observer used to cast primary rays; independent of any GPU camera type.</summary>
public readonly struct RenderCamera
{
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

    public Vector3 Position { get; }

    public Vector3 Forward { get; }

    public Vector3 Right { get; }

    public Vector3 Up { get; }

    public float VerticalFovRadians { get; }

    public float AspectRatio { get; }

    /// <summary>Builds a camera from eye, look target, and up (typical game / simulation observer).</summary>
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
    public static RenderCamera FromObserver(
        Vector3 position,
        Vector3 target,
        Vector3 up,
        float fieldOfViewDegrees,
        float aspectRatio) =>
        LookAt(position, target, up, fieldOfViewDegrees, aspectRatio);
}
