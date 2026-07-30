using System.Numerics;
using System.Runtime.InteropServices;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>World-space line vertex for <see cref="VulkanWireframeRenderer"/>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct VulkanWireVertex
{
    /// <summary>World position.</summary>
    public Vector3 Position;

    /// <summary>Linear RGBA (0..1).</summary>
    public Vector4 Color;

    /// <summary>Creates a vertex with an 8-bit RGBA color.</summary>
    public static VulkanWireVertex From(Vector3 position, byte r, byte g, byte b, byte a = 255) =>
        new()
        {
            Position = position,
            Color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f),
        };
}
