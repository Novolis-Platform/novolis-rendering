using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>Blittable camera for ILGPU kernels.</summary>
public struct IlgpuCameraParams
{
    public Float3 Position;
    public Float3 Forward;
    public Float3 Right;
    public Float3 Up;
    public float TanHalfFov;
    public float Aspect;

    public static IlgpuCameraParams FromSnapshot(CameraSnapshot camera, int width, int height) =>
        new()
        {
            Position = Float3.From(camera.Position),
            Forward = Float3.From(camera.Forward),
            Right = Float3.From(camera.Right),
            Up = Float3.From(camera.Up),
            TanHalfFov = MathF.Tan(camera.VerticalFovRadians * 0.5f),
            Aspect = width / (float)height,
        };
}
