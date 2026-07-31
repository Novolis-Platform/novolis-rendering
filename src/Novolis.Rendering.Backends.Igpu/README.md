# Novolis.Rendering.Backends.Igpu

ILGPU compute path tracing with automatic CPU fallback when no suitable GPU is present or when deterministic mode is requested.

## Install

```bash
dotnet add package Novolis.Rendering.Backends.Igpu
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), CUDA/OpenCL-capable GPU optional. Configure device via `NOVOLIS_ILGPU_DEVICE` (see demo title formatting in `PathTraceStatusTitle`).

## Quick start

```csharp
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Runtime;

await using var backend = new IlgpuRayTracingBackend();
await backend.ResizeAsync(1280, 720);
await backend.UploadSceneAsync(compiledScene);

await backend.RenderAsync(camera, sampleIndex: 0);
backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
```

Default backend when `NOVOLIS_RAY_BACKEND` is unset. Implements `IRayTracingBackend` and `IDisposable`.

## API

| Type | Role |
|------|------|
| `IlgpuRayTracingBackend` | GPU/CPU ILGPU path tracer |
| `IlgpuCameraParams` | Blittable camera params for kernels |
| `GpuBvhNode` | GPU BVH node layout |
| `Float3` | ILGPU-friendly 3-vector |

## Dogfooding / apps

Default interactive backend in path-tracing demos. Switch at runtime with `PathTraceSession.CycleBackend()` or `UseIlgpuBackend()` in DI.

## Support

Pre-release platform library. GPU availability depends on ILGPU runtime and drivers.

Set `NOVOLIS_ILGPU_DEVICE` to pin a specific accelerator when multiple are present.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Backends.Cpu` | Pure CPU reference and deterministic tests |
| `Novolis.Rendering.Backends.Vulkan` | Vulkan compute alternative |
| `Novolis.Rendering.DependencyInjection` | `UseIlgpuBackend()` registration |
| `Novolis.Rendering.PathTrace.Demos` | `PathTraceBackendFactory`, session helpers |

## More documentation

- [Roadmap: ray tracing](../../docs/roadmap-raytracing.md)
- [Materials and backends](../../docs/materials-and-backends.md)
