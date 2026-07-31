# Novolis.Rendering.Backends.Vulkan

Vulkan compute path tracing (SPIR-V kernels) with CPU-readable output via staging buffers when a Vulkan 1.2+ runtime is available.

## Install

```bash
dotnet add package Novolis.Rendering.Backends.Vulkan
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Vulkan 1.2+ loader and compatible GPU.

## Quick start

```csharp
using Novolis.Rendering.Backends.Vulkan;
using Novolis.Rendering.Runtime;

await using var backend = new VulkanRayTracingBackend();
await backend.ResizeAsync(1280, 720);
await backend.UploadSceneAsync(compiledScene);

await backend.RenderAsync(camera, sampleIndex: 0);
backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
```

Select with `NOVOLIS_RAY_BACKEND=vulkan`, `PathTraceBackendKind.Vulkan`, or `UseVulkanBackend()` in DI. `GpuSurface` may expose `ICpuBackedGpuSurface` for GPU-native presentation paths.

## API

| Type | Role |
|------|------|
| `VulkanRayTracingBackend` | `IRayTracingBackend`; compute path tracer |
| `VulkanWireframeRenderer` | Optional wireframe overlay helper |
| `VulkanWireVertex` | Wireframe vertex layout |

## Dogfooding / apps

Hot-switched in path-tracing demos via `PathTraceSession.SwitchBackend(PathTraceBackendKind.Vulkan)`. Wireframe helpers support debug overlays in studio samples.

SPIR-V compute kernels ship embedded; no external shader files at runtime.

See `NOVOLIS_RAY_BACKEND=vulkan` for demo backend selection.

Implements `IRayTracingBackend` and `IDisposable`.

## Support

Pre-release platform library. Requires a working Vulkan loader; CPU readback uses staging when available.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Backends.Igpu` | ILGPU instead of Vulkan |
| `Novolis.Rendering.Presentation.Abstractions` | `ICpuBackedGpuSurface`, `IGpuFramePresenter` |
| `Novolis.Rendering.DependencyInjection` | `UseVulkanBackend()` |
| `Novolis.Rendering.PathTrace.Demos` | Backend factory and hot-switch |

## More documentation

- [Roadmap: ray tracing](../../docs/roadmap-raytracing.md)
