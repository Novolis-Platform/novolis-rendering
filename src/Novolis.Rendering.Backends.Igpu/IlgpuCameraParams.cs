using System.Numerics;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>Blittable camera for ILGPU kernels.</summary>
public readonly struct IlgpuCameraParams
{
    public Vector3 Position { get; init; }
    public Vector3 Forward { get; init; }
    public Vector3 Right { get; init; }
    public Vector3 Up { get; init; }
    public float TanHalfFov { get; init; }
    public float Aspect { get; init; }

    public static IlgpuCameraParams FromSnapshot(CameraSnapshot camera, int width, int height) =>
        new()
        {
            Position = camera.Position,
            Forward = camera.Forward,
            Right = camera.Right,
            Up = camera.Up,
            TanHalfFov = MathF.Tan(camera.VerticalFovRadians * 0.5f),
            Aspect = width / (float)height,
        };
}
