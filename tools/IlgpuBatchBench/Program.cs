using System.Diagnostics;
using System.Numerics;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.PathTrace.Demos;
using Novolis.Rendering.Runtime;

const int Width = 1280;
const int Height = 720;
const int Warmup = 2;
const int Samples = 32;

using var backend = new IlgpuRayTracingBackend();
Console.WriteLine($"backend={backend.BackendLabel}");

await Bench("UnitCubeRoom", DemoSceneFactory.UnitCubeRoom(), Width, Height);
await Bench("StudioShowcase", ShowcaseScenes.BuildStudioShowcase(), Width, Height);
var (iw, ih) = InteractiveTraceSize.Clamp(Width, Height);
await Bench("StudioShowcase@interactive", ShowcaseScenes.BuildStudioShowcase(), iw, ih);
return;

async Task Bench(string name, CompiledScene scene, int width, int height)
{
    var camera = CameraSnapshot.LookAt(
        new Vector3(1.2f, 0.8f, 2f),
        new Vector3(0f, 0.2f, 0f),
        Vector3.UnitY,
        55f,
        width / (float)height);

    await backend.ResizeAsync(width, height);
    await backend.UploadSceneAsync(scene);

    for (var i = 0; i < Warmup; i++)
    {
        backend.ResetAccumulation();
        await backend.RenderBatchAsync(camera, 0, 4);
        _ = backend.Output.TryGetCpuPixels(out _, out _, out _);
    }

    backend.ResetAccumulation();
    var swBatch = Stopwatch.StartNew();
    await backend.RenderBatchAsync(camera, 0, Samples);
    _ = backend.Output.TryGetCpuPixels(out _, out _, out _);
    swBatch.Stop();

    backend.ResetAccumulation();
    var swNaive = Stopwatch.StartNew();
    for (var i = 0; i < Samples; i++)
    {
        await backend.RenderAsync(camera, i);
        _ = backend.Output.TryGetCpuPixels(out _, out _, out _);
    }

    swNaive.Stop();

    Console.WriteLine($"--- {name} {width}x{height} samples={Samples} ---");
    Console.WriteLine($"batch_ms={swBatch.Elapsed.TotalMilliseconds:F1}  ({Samples / swBatch.Elapsed.TotalSeconds:F1} spp/s)");
    Console.WriteLine($"naive_ms={swNaive.Elapsed.TotalMilliseconds:F1}  ({Samples / swNaive.Elapsed.TotalSeconds:F1} spp/s)");
    Console.WriteLine($"speedup={swNaive.Elapsed.TotalMilliseconds / Math.Max(1e-3, swBatch.Elapsed.TotalMilliseconds):F2}x");
}
