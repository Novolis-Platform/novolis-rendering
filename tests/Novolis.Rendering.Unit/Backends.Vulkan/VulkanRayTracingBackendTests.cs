using System.Numerics;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Vulkan;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Testing;
using TUnit.Core;

namespace Novolis.Rendering.Backends.Vulkan.Tests;

public sealed class VulkanRayTracingBackendTests
{
    [Test]
    public async Task RenderAsync_ExposesCpuBackedGpuSurface()
    {
        using var backend = new VulkanRayTracingBackend();
        if (backend.BackendLabel.Contains("fallback", StringComparison.Ordinal))
        {
            return;
        }

        var scene = DemoSceneFactory.UnitCubeRoom();
        var camera = CameraSnapshot.LookAt(
            new Vector3(1.2f, 0.8f, 2f),
            new Vector3(0f, 0.2f, 0f),
            Vector3.UnitY,
            55f,
            1f);

        await backend.ResizeAsync(8, 8);
        await backend.UploadSceneAsync(scene);
        await backend.RenderAsync(camera, 0);

        await Assert.That(backend.GpuSurface).IsNotNull();
        await Assert.That(backend.GpuSurface).IsTypeOf<ICpuBackedGpuSurface>();
        var surface = (ICpuBackedGpuSurface)backend.GpuSurface!;
        var hasPixels = surface.TryGetCpuPixels(out _, out var w, out var h);
        await Assert.That(hasPixels).IsTrue();
        await Assert.That(w).IsEqualTo(8);
        await Assert.That(h).IsEqualTo(8);
        await Assert.That(backend.BackendLabel).StartsWith("Vulkan");
        await Assert.That(surface.NativeHandle).IsNotEqualTo(nint.Zero);
    }
}
