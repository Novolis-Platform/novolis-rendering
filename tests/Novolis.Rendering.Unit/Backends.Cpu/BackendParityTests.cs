using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Testing;
using TUnit.Core;

namespace Novolis.Rendering.Backends.Cpu.Tests;

public sealed class BackendParityTests
{
    /// <summary>Uses CPU fallback; does not exercise ILGPU buffer upload. See <c>IlgpuSceneBufferTests</c>.</summary>
    [Test]
    public async Task Ilgpu_MatchesCpu_OnDeterministicFrame()
    {
        var scene = DemoSceneFactory.UnitCubeRoom();
        var camera = CameraSnapshot.LookAt(
            new(1.2f, 0.8f, 2f),
            System.Numerics.Vector3.Zero,
            System.Numerics.Vector3.UnitY,
            55f,
            32f / 24f);

        var cpu = new CpuRayTracingBackend(deterministic: true);
        await cpu.ResizeAsync(32, 24);
        await cpu.UploadSceneAsync(scene);
        await cpu.RenderAsync(camera, 0);
        await cpu.RenderAsync(camera, 1);

        using var igpu = new IlgpuRayTracingBackend(deterministic: true);
        await igpu.ResizeAsync(32, 24);
        await igpu.UploadSceneAsync(scene);
        await igpu.RenderAsync(camera, 0);
        await igpu.RenderAsync(camera, 1);

        var hash = FramebufferGoldenAssert.Sha256Hex(cpu.Output);
        await Assert.That(FramebufferGoldenAssert.Sha256Hex(igpu.Output)).IsEqualTo(hash);
        await Assert.That(hash).IsEqualTo("e1f7c0113c5708b9d6e7aa177f8dd08e78b4897b429b2d2b2f3d3a144efcaa2f");
    }
}
