using System.Numerics;
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
    public async Task TracePixelKernel_CompilesOnIlgpuAccelerator()
    {
        using var context = ILGPU.Context.CreateDefault();
        var device = context.Devices.FirstOrDefault(d => d.AcceleratorType == AcceleratorType.Cuda)
            ?? context.Devices.FirstOrDefault(d => d.AcceleratorType == AcceleratorType.CPU);
        if (device is null)
        {
            return;
        }

        using var accelerator = device.CreateAccelerator(context);
        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, int, int, int, IlgpuCameraParams,
            ArrayView<Float3>, ArrayView<byte>, ArrayView<GpuTriangle>, ArrayView<GpuMaterial>, ArrayView<GpuLight>,
            int, ArrayView<GpuBvhNode>, int, ArrayView<int>>(IlgpuPathTracerKernels.TracePixelKernel);

        await Assert.That(kernel).IsNotNull();
    }

    [Test]
    public async Task IlgpuRayTracingBackend_RenderOneSample_WhenGpuPathActive()
    {
        using var backend = new IlgpuRayTracingBackend();
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
            16f / 16f);

        await backend.ResizeAsync(16, 16);
        await backend.UploadSceneAsync(scene);
        await backend.RenderAsync(camera, 0);

        await Assert.That(backend.SampleCount).IsEqualTo(1);
        backend.Output.TryGetCpuPixels(out var pixels, out _, out _);
        var maxR = 0;
        foreach (var p in pixels)
        {
            maxR = System.Math.Max(maxR, p.R);
        }

        await Assert.That(maxR).IsGreaterThan(0);
    }
}
