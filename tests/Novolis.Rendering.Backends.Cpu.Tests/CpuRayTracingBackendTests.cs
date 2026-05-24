using System.Numerics;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Testing;
using TUnit.Core;

namespace Novolis.Rendering.Backends.Cpu.Tests;

public sealed class CpuRayTracingBackendTests
{
    [Test]
    public async Task Render_Progressive_IncreasesSampleCount()
    {
        var backend = new CpuRayTracingBackend(deterministic: true);
        await backend.ResizeAsync(32, 24);
        await backend.UploadSceneAsync(DemoSceneFactory.UnitCubeRoom());
        var camera = CameraSnapshot.LookAt(
            new Vector3(1.2f, 0.8f, 2f),
            Vector3.Zero,
            Vector3.UnitY,
            55f,
            32f / 24f);
        await backend.RenderAsync(camera, 0);
        await backend.RenderAsync(camera, 1);
        await Assert.That(backend.SampleCount).IsEqualTo(2);
        var hash = FramebufferGoldenAssert.Sha256Hex(backend.Output);
        await Assert.That(hash.Length).IsEqualTo(64);
    }

    [Test]
    public async Task Render_HitsGeometry()
    {
        var backend = new CpuRayTracingBackend(deterministic: true);
        await backend.ResizeAsync(64, 48);
        await backend.UploadSceneAsync(DemoSceneFactory.UnitCubeRoom());
        var camera = CameraSnapshot.LookAt(
            new Vector3(1.2f, 0.8f, 2f),
            new Vector3(0f, 0.2f, 0f),
            Vector3.UnitY,
            55f,
            64f / 48f);
        await backend.RenderAsync(camera, 0);
        backend.Output.TryGetCpuPixels(out var pixels, out _, out _);
        var center = pixels[32 * 48 + 32];
        await Assert.That((int)center.R).IsGreaterThan(30);
    }
}
