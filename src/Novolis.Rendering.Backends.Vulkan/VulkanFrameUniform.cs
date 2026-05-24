using System.Numerics;
using System.Runtime.InteropServices;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Vulkan;

[StructLayout(LayoutKind.Sequential)]
internal struct VulkanFrameUniform
{
    public int Width;
    public int Height;
    public int SampleIndex;
    public int LightCount;
    public int BvhRootIndex;
    public Vector3 CamPos;
    public float PadCam0;
    public Vector3 CamForward;
    public float PadCam1;
    public Vector3 CamRight;
    public float PadCam2;
    public Vector3 CamUp;
    public float TanHalfFov;
    public float Aspect;

    public static VulkanFrameUniform From(CameraSnapshot camera, int width, int height, int sampleIndex, int lightCount, int bvhRootIndex) =>
        new()
        {
            Width = width,
            Height = height,
            SampleIndex = sampleIndex,
            LightCount = lightCount,
            BvhRootIndex = bvhRootIndex,
            CamPos = camera.Position,
            CamForward = camera.Forward,
            CamRight = camera.Right,
            CamUp = camera.Up,
            TanHalfFov = MathF.Tan(camera.VerticalFovRadians * 0.5f),
            Aspect = width / (float)height,
        };
}
