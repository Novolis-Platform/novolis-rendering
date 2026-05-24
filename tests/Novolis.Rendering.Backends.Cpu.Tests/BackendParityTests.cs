using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Testing;
using TUnit.Core;

namespace Novolis.Rendering.Backends.Cpu.Tests;

public sealed class BackendParityTests
{
    [Test]
    public async Task Ilgpu_MatchesCpu_OnDeterministicFrame()
    {
        var scene = DemoSceneFactory.UnitCubeRoom();
        var camera = CameraSnapshot.LookAt(
            new(1.2f, 0.8f, 2f),
            new(0f, 0.2f, 0f),
            System.Numerics.Vector3.UnitY,
            55f,
            32f / 24f);

        var cpu = new CpuRayTracingBackend(deterministic: true);
        await cpu.ResizeAsync(32, 24);
        await cpu.UploadSceneAsync(scene);
        await cpu.RenderAsync(camera, 0);

        using var igpu = new IlgpuRayTracingBackend(deterministic: true);
        await igpu.ResizeAsync(32, 24);
        await igpu.UploadSceneAsync(scene);
        await igpu.RenderAsync(camera, 0);

        await Assert.That(FramebufferGoldenAssert.Sha256Hex(igpu.Output))
            .IsEqualTo(FramebufferGoldenAssert.Sha256Hex(cpu.Output));
    }
}
