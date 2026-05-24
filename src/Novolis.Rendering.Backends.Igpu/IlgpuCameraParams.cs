using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>Blittable camera for ILGPU kernels.</summary>
public struct IlgpuCameraParams
{
    /// <summary>Eye position.</summary>
    public Float3 Position;

    /// <summary>View direction.</summary>
    public Float3 Forward;

    /// <summary>Camera right axis.</summary>
    public Float3 Right;

    /// <summary>Camera up axis.</summary>
    public Float3 Up;

    /// <summary>Tangent of half vertical field of view.</summary>
    public float TanHalfFov;

    /// <summary>Width divided by height.</summary>
    public float Aspect;

    /// <summary>Packs a <see cref="CameraSnapshot"/> for kernel consumption.</summary>
    /// <param name="camera">Observer snapshot.</param>
    /// <param name="width">Framebuffer width.</param>
    /// <param name="height">Framebuffer height.</param>
    /// <returns>Blittable camera parameters.</returns>
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
