using ILGPU;
using ILGPU.Runtime;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Runtime;
using TUnit.Core;

namespace Novolis.Rendering.Backends.Igpu.Tests;

public sealed class IlgpuSceneBufferTests
{
    [Test]
    public async Task CompiledScene_GpuBuffers_AllocateOnIlgpuAccelerator()
    {
        var scene = DemoSceneFactory.UnitCubeRoom();
        using var context = ILGPU.Context.CreateDefault();
        using var accelerator = context.GetPreferredDevice(preferCPU: true).CreateAccelerator(context);

        using var triangles = accelerator.Allocate1D(scene.Triangles.ToArray());
        using var materials = accelerator.Allocate1D(scene.Materials.ToArray());
        using var lights = scene.Lights.IsEmpty
            ? accelerator.Allocate1D<GpuLight>(1)
            : accelerator.Allocate1D(scene.Lights.ToArray());
        using var bvh = scene.BvhNodes.IsEmpty
            ? accelerator.Allocate1D<GpuBvhNode>(1)
            : accelerator.Allocate1D(scene.BvhNodes.Select(GpuBvhNode.From).ToArray());
        using var order = scene.TriangleOrder.IsEmpty
            ? accelerator.Allocate1D<int>(1)
            : accelerator.Allocate1D(scene.TriangleOrder.ToArray());

        await Assert.That(triangles.Length).IsGreaterThan(0);
        await Assert.That(bvh.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task IlgpuRayTracingBackend_UploadScene_DoesNotThrow_WhenGpuPathActive()
    {
        using var backend = new IlgpuRayTracingBackend();
        if (backend.BackendLabel.Contains("fallback", StringComparison.Ordinal))
        {
            return;
        }

        await backend.ResizeAsync(16, 16);
        await backend.UploadSceneAsync(DemoSceneFactory.UnitCubeRoom());
        await Assert.That(backend.SampleCount).IsEqualTo(0);
    }
}
