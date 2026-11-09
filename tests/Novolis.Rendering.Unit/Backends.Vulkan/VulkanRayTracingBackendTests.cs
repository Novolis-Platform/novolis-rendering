using System.Numerics;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Vulkan;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Testing;
using TUnit.Core;

namespace Novolis.Rendering.Backends.Vulkan.Tests;

public sealed class VulkanRayTracingBackendTests
{
    [Test]
    public async Task BackendLabel_DocumentsCpuFallback_BecauseSilkOwnsVulkan()
    {
        using var backend = new VulkanRayTracingBackend();
        await Assert.That(backend.BackendLabel).IsEqualTo("Vulkan (CPU fallback)");
        await Assert.That(backend.GpuSurface).IsNull();
    }

    [Test]
    public async Task Deterministic_MatchesCpu_OnGoldenFrame()
    {
        var scene = DemoSceneFactory.UnitCubeRoom();
        var camera = CameraSnapshot.LookAt(
            new Vector3(1.2f, 0.8f, 2f),
            Vector3.Zero,
            Vector3.UnitY,
            55f,
            32f / 24f);

        var cpu = new CpuRayTracingBackend(deterministic: true);
        await cpu.ResizeAsync(32, 24);
        await cpu.UploadSceneAsync(scene);
        await cpu.RenderAsync(camera, 0);
        await cpu.RenderAsync(camera, 1);

        using var vulkan = new VulkanRayTracingBackend(deterministic: true);
        await vulkan.ResizeAsync(32, 24);
        await vulkan.UploadSceneAsync(scene);
        await vulkan.RenderAsync(camera, 0);
        await vulkan.RenderAsync(camera, 1);

        await Assert.That(FramebufferGoldenAssert.Sha256Hex(vulkan.Output))
            .IsEqualTo(FramebufferGoldenAssert.Sha256Hex(cpu.Output));
        await Assert.That(FramebufferGoldenAssert.Sha256Hex(vulkan.Output))
            .IsEqualTo("e1f7c0113c5708b9d6e7aa177f8dd08e78b4897b429b2d2b2f3d3a144efcaa2f");
    }
}
