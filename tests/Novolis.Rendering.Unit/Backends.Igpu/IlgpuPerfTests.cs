using System.Diagnostics;
using System.Numerics;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Runtime;
using TUnit.Core;

namespace Novolis.Rendering.Backends.Igpu.Tests;

public sealed class IlgpuPerfTests
{
    [Test]
    public async Task RenderBatch_Outpaces_PerSampleFlush_OnGpuPath()
    {
        using var backend = new IlgpuRayTracingBackend();
        if (backend.BackendLabel.Contains("fallback", StringComparison.Ordinal))
        {
            // CI / machines without a working CUDA ILGPU path — skip perf gate.
            await Assert.That(backend.BackendLabel).Contains("fallback");
            return;
        }

        const int width = 640;
        const int height = 360;
        const int samples = 16;
        var scene = DemoSceneFactory.UnitCubeRoom();
        var camera = CameraSnapshot.LookAt(
            new Vector3(1.2f, 0.8f, 2f),
            new Vector3(0f, 0.2f, 0f),
            Vector3.UnitY,
            55f,
            width / (float)height);

        await backend.ResizeAsync(width, height);
        await backend.UploadSceneAsync(scene);

        backend.ResetAccumulation();
        await backend.RenderBatchAsync(camera, 0, 2);
        _ = backend.Output.TryGetCpuPixels(out _, out _, out _);

        backend.ResetAccumulation();
        var batch = Stopwatch.StartNew();
        await backend.RenderBatchAsync(camera, 0, samples);
        _ = backend.Output.TryGetCpuPixels(out _, out _, out _);
        batch.Stop();

        backend.ResetAccumulation();
        var naive = Stopwatch.StartNew();
        for (var i = 0; i < samples; i++)
        {
            await backend.RenderAsync(camera, i);
            _ = backend.Output.TryGetCpuPixels(out _, out _, out _);
        }

        naive.Stop();

        await Assert.That(backend.SampleCount).IsEqualTo(samples);
        // Batch must beat naive flush; allow noise but require a clear win.
        await Assert.That(batch.Elapsed.TotalMilliseconds * 1.25)
            .IsLessThan(naive.Elapsed.TotalMilliseconds);
    }
}
