<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-rendering">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.Runtime

Flat runtime scene data and the `IRayTracingBackend` contract shared by CPU, ILGPU, and Vulkan path tracers. Host-neutral — no window or GPU draw calls.

## Install

```bash
dotnet add package Novolis.Rendering.Runtime
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Rendering.Presentation.Abstractions`.

## Quick start

```csharp
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Runtime;

IRayTracingBackend backend = new CpuRayTracingBackend();
await backend.ResizeAsync(640, 480);
await backend.UploadSceneAsync(compiledScene);

var camera = CameraSnapshot.LookAt(
    new Vector3(1.2f, 0.8f, 2f), Vector3.Zero, Vector3.UnitY, 60f, 640f / 480f);

await backend.RenderAsync(camera, sampleIndex: 0);
backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
```

Progressive rendering: increment `sampleIndex` each frame or call `ResetAccumulation()` when the camera moves. `SampleCount` reflects integrated samples since the last reset.

## Quick start — ray math

```csharp
var dir = CameraSnapshotViewBasis.PrimaryRayDirection(in camera, u: 0.5f, v: 0.5f);
```

## API

| Type | Role |
|------|------|
| `IRayTracingBackend` | Resize, upload, render, output, accumulation |
| `CompiledScene` | Triangles, materials, lights, BVH nodes/order |
| `CameraSnapshot` | Observer pose + FOV; `LookAt`, `FromObserver` |
| `CameraSnapshotViewBasis` | Primary ray direction helpers |
| `GpuTriangle` | World triangle + material index |
| `GpuMaterial` / `MaterialModel` | Blittable shading parameters |
| `GpuLight` / `GpuLightKind` | Runtime light records |

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Compile` | Build `CompiledScene` from authoring `Scene` |
| `Novolis.Rendering.Backends.Cpu` | Reference CPU path tracer |
| `Novolis.Rendering.Backends.Igpu` | ILGPU compute backend |
| `Novolis.Rendering.Backends.Vulkan` | Vulkan compute backend |
| `Novolis.Rendering.Presentation.Abstractions` | `IRenderOutput` for presenters |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Roadmap: ray tracing](../../docs/roadmap-raytracing.md)

