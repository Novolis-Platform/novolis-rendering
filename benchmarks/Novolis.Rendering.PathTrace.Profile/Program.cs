using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.PathTrace.Demos;
using Novolis.Rendering.Runtime;

// Toub-style stage profile: wall time + allocations per hot path (run Release).
var argv = args;
if (HasFlag(argv, "--help") || HasFlag(argv, "-h"))
{
    PrintHelp();
    return 0;
}

var width = ArgInt(argv, "--width", 320);
var height = ArgInt(argv, "--height", 180);
var samples = ArgInt(argv, "--samples", 4);
var iters = ArgInt(argv, "--iters", 64);
var warmup = ArgInt(argv, "--warmup", 8);
var json = HasFlag(argv, "--json");

using var backend = new IlgpuRayTracingBackend();
Console.WriteLine($"backend={backend.BackendLabel}");
if (backend.BackendLabel.Contains("fallback", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("GPU path unavailable — refusing to profile CPU fallback as ILGPU orbit cost.");
    return 2;
}

var scene = ShowcaseScenes.BuildStudioShowcase();
var camera = CameraSnapshot.LookAt(
    ShowcaseScenes.StudioDefaultEye,
    ShowcaseScenes.OrbitTarget,
    Vector3.UnitY,
    52f,
    width / (float)height);

await backend.ResizeAsync(width, height);
await backend.UploadSceneAsync(scene);

var display = new PathTraceDisplayBuffer();
display.Invalidate(width, height);

Console.WriteLine($"scene=StudioShowcase size={width}x{height} samples/batch={samples} iters={iters} warmup={warmup}");
Console.WriteLine();

for (var i = 0; i < warmup; i++)
{
    backend.ResetAccumulation();
    await backend.RenderBatchAsync(camera, 0, samples, flushToCpu: true);
    _ = backend.Output.TryGetCpuPixels(out _, out _, out _);
}

var stages = new List<StageResult>
{
    Measure(
        "reset",
        iters,
        body: () => backend.ResetAccumulation()),
    Measure(
        "enqueue-only",
        iters,
        body: () =>
        {
            backend.ResetAccumulation();
            backend.RenderBatchAsync(camera, 0, samples, flushToCpu: false).GetAwaiter().GetResult();
        }),
    Measure(
        "exec+readback",
        iters,
        prepare: () =>
        {
            backend.ResetAccumulation();
            backend.RenderBatchAsync(camera, 0, samples, flushToCpu: false).GetAwaiter().GetResult();
        },
        body: () => backend.FlushCpuDisplay()),
    Measure(
        "batch+readback",
        iters,
        body: () =>
        {
            backend.ResetAccumulation();
            backend.RenderBatchAsync(camera, 0, samples, flushToCpu: true).GetAwaiter().GetResult();
        }),
    Measure(
        "publish",
        iters,
        prepare: () =>
        {
            backend.ResetAccumulation();
            backend.RenderBatchAsync(camera, 0, samples, flushToCpu: true).GetAwaiter().GetResult();
        },
        body: () =>
        {
            _ = backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
            display.Publish(pixels, w, h, backend.SampleCount);
        }),
    Measure(
        "copy-if-new",
        iters,
        prepare: () =>
        {
            backend.ResetAccumulation();
            backend.RenderBatchAsync(camera, 0, samples, flushToCpu: true).GetAwaiter().GetResult();
            _ = backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
            display.Publish(pixels, w, h, backend.SampleCount);
        },
        body: () =>
        {
            _ = backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
            display.Publish(pixels, w, h, backend.SampleCount);
            var gen = -1;
            _ = display.TryCopyFrameIfNew(ref gen, out _, out _, out _);
        }),
    Measure(
        "moving-1spp-loop",
        iters,
        body: () =>
        {
            backend.RenderBatchAsync(camera, 0, 1, flushToCpu: true).GetAwaiter().GetResult();
            _ = backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
            display.Publish(pixels, w, h, backend.SampleCount);
        }),
    Measure(
        "orbit-bucket-4spp",
        iters,
        body: () =>
        {
            backend.RenderBatchAsync(camera, 0, 4, flushToCpu: true).GetAwaiter().GetResult();
            _ = backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
            display.Publish(pixels, w, h, backend.SampleCount);
        }),
};

PrintReport(stages, width, height, samples, json);
return 0;

static StageResult Measure(string name, int iters, Action body, Action? prepare = null)
{
    // Prime once so steady-state dominates.
    prepare?.Invoke();
    body();

    var allocBefore = GC.GetAllocatedBytesForCurrentThread();
    var bodyMs = 0.0;
    var prepareMs = 0.0;
    for (var i = 0; i < iters; i++)
    {
        if (prepare is not null)
        {
            var prepSw = Stopwatch.StartNew();
            prepare();
            prepSw.Stop();
            prepareMs += prepSw.Elapsed.TotalMilliseconds;
        }

        var bodySw = Stopwatch.StartNew();
        body();
        bodySw.Stop();
        bodyMs += bodySw.Elapsed.TotalMilliseconds;
    }

    var allocAfter = GC.GetAllocatedBytesForCurrentThread();
    var seconds = bodyMs / 1000.0;
    return new StageResult(
        name,
        iters,
        bodyMs,
        bodyMs / iters,
        iters / (seconds <= 0 ? 1e-9 : seconds),
        Math.Max(0, allocAfter - allocBefore),
        Math.Max(0, allocAfter - allocBefore) / (double)iters,
        prepareMs / Math.Max(1, iters));
}

static void PrintReport(List<StageResult> stages, int width, int height, int samples, bool json)
{
    if (json)
    {
        var sb = new StringBuilder();
        sb.Append("{\"width\":").Append(width)
            .Append(",\"height\":").Append(height)
            .Append(",\"samples\":").Append(samples)
            .Append(",\"stages\":[");
        for (var i = 0; i < stages.Count; i++)
        {
            var s = stages[i];
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append('{')
                .Append("\"name\":\"").Append(s.Name).Append('"')
                .Append(",\"iters\":").Append(s.Iters)
                .Append(",\"totalMs\":").Append(Inv(s.TotalMs))
                .Append(",\"avgMs\":").Append(Inv(s.AvgMs))
                .Append(",\"hz\":").Append(Inv(s.Hz))
                .Append(",\"allocBytes\":").Append(s.AllocBytes)
                .Append(",\"allocPerOp\":").Append(Inv(s.AllocPerOp))
                .Append('}');
        }

        sb.Append("]}");
        Console.WriteLine(sb);
        return;
    }

    Console.WriteLine($"{"stage",-22} {"iters",6} {"avg_ms",10} {"hz",10} {"alloc/op",12} {"prep_ms",10}");
    Console.WriteLine(new string('-', 78));
    foreach (var s in stages.OrderByDescending(x => x.AvgMs))
    {
        Console.WriteLine(
            $"{s.Name,-22} {s.Iters,6} {s.AvgMs,10:F3} {s.Hz,10:F1} {FormatBytes(s.AllocPerOp),12} {s.PrepareAvgMs,10:F3}");
    }

    Console.WriteLine();
    var moving = stages.First(s => s.Name == "moving-1spp-loop");
    var readback = stages.First(s => s.Name == "exec+readback");
    var enqueue = stages.First(s => s.Name == "enqueue-only");
    var presentShare = 100.0 * readback.AvgMs / Math.Max(1e-6, enqueue.AvgMs + readback.AvgMs);
    Console.WriteLine(
        $"derived: moving_present_hz≈{moving.Hz:F1}  enqueue={enqueue.AvgMs:F3}ms  exec+readback={readback.AvgMs:F3}ms  readback_share≈{presentShare:F0}%");
    Console.WriteLine("note: enqueue-only does not wait for the GPU; exec+readback pays deferred kernel time + DMA.");
}

static string Inv(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

static string FormatBytes(double bytes) =>
    bytes < 1024 ? $"{bytes:F0} B" :
    bytes < 1024 * 1024 ? $"{bytes / 1024:F1} KB" :
    $"{bytes / (1024 * 1024):F2} MB";

static void PrintHelp()
{
    Console.WriteLine("""
        Novolis.Rendering.PathTrace.Profile — stage profiler for ILGPU interactive path trace.

          --width N       Trace width (default 480)
          --height N      Trace height (default 270)
          --samples N     Samples per batch stage (default 4)
          --iters N       Timed iterations (default 64)
          --warmup N      Warmup iterations (default 8)
          --json          Machine-readable summary

        Example:
          dotnet run -c Release --project d:\novolis\novolis-rendering\benchmarks\Novolis.Rendering.PathTrace.Profile\Novolis.Rendering.PathTrace.Profile.csproj -p:NovolisUseProjectReferences=true
        """);
}

static bool HasFlag(string[] argv, string name) =>
    argv.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

static int ArgInt(string[] argv, string name, int fallback)
{
    for (var i = 0; i < argv.Length - 1; i++)
    {
        if (string.Equals(argv[i], name, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(argv[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            && value > 0)
        {
            return value;
        }
    }

    return fallback;
}

readonly record struct StageResult(
    string Name,
    int Iters,
    double TotalMs,
    double AvgMs,
    double Hz,
    long AllocBytes,
    double AllocPerOp,
    double PrepareAvgMs);
