using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>
/// CAD wireframe GPU path moved to <c>Novolis.Silk.Compute</c>. TryCreate always fails so hosts fall back.
/// </summary>
public sealed class VulkanWireframeRenderer : IDisposable
{
    /// <summary>Always returns false; GPU wireframe is composed through Silk.</summary>
    public static bool TryCreate(out VulkanWireframeRenderer? renderer)
    {
        renderer = null;
        return false;
    }

    /// <summary>Throws because the Silk.NET Vulkan device no longer lives in Rendering.</summary>
    public static VulkanWireframeRenderer Create() =>
        throw new InvalidOperationException("Vulkan wireframe GPU moved to Novolis.Silk.Compute; compose in the host.");

    /// <summary>No-op (renderer is never constructed).</summary>
    public void Resize(int width, int height)
    {
    }

    /// <summary>No-op (renderer is never constructed).</summary>
    public void Render(ReadOnlySpan<VulkanWireVertex> lines, Matrix4x4 mvp, Rgba32 clear)
    {
    }

    /// <summary>No-op (renderer is never constructed).</summary>
    public void Readback(Span<Rgba32> destination)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
