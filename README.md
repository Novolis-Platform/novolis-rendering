# novolis-rendering

**Graphics-host-neutral ray tracing** — authoring, compilation, CPU/GPU backends, and framebuffer contracts. Computes RGBA frames; **does not** own windows, GPU draw calls, or input.

Display adapters (Raylib texture upload, Silk.NET, …) live in host repos. See [library boundaries](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md).

## Pipeline

```text
Scene + IMaterial  →  SceneCompiler  →  CompiledScene
  →  IRayTracingBackend  →  IRenderOutput (CPU pixels)
  →  IFramePresenter (Raylib / Silk in host repos)
```

## Packages

| Package | Role |
|---------|------|
| `Novolis.Rendering.Abstractions` | Legacy bootstrap types (obsolete); shared primitives |
| `Novolis.Rendering.Presentation.Abstractions` | `IFramePresenter`, `IRenderOutput` |
| `Novolis.Rendering.Materials` | `IMaterial`, presets, `MaterialCompiler` → `GpuMaterial` |
| `Novolis.Rendering.Scene` | Authoring `Scene`, `MeshInstance`, lights |
| `Novolis.Rendering.Compile` | `SceneCompiler` + BVH build |
| `Novolis.Rendering.Runtime` | `CompiledScene`, `CameraSnapshot`, `IRayTracingBackend` |
| `Novolis.Rendering.Backends.Cpu` | CPU path tracer (progressive accumulation) |
| `Novolis.Rendering.Backends.Igpu` | ILGPU backend (CPU fallback until kernels ship) |
| `Novolis.Rendering.Backends.Vulkan` | Vulkan compute placeholder |
| `Novolis.Rendering.DependencyInjection` | `AddRayTracing()`, `UseCpuBackend()` |
| `Novolis.Rendering.Presentation.Silk` | Silk CPU presenter stub |
| `Novolis.Rendering` | Meta package referencing the stack |

Normative API: [docs/materials-and-backends.md](docs/materials-and-backends.md). Roadmap: [docs/roadmap-raytracing.md](docs/roadmap-raytracing.md).

## Build

```powershell
cd d:\novolis\novolis-math
.\scripts\pack-local.ps1
cd d:\novolis\novolis-rendering
.\scripts\ci-pack-dependencies.ps1   # or pack-local.ps1 for full stack
dotnet build Novolis.Rendering.slnx -c Release
dotnet run --project tests/Novolis.Rendering.Backends.Cpu.Tests -c Release
```

CI checks out `novolis-math` and runs `scripts/ci-pack-dependencies.ps1` before restore.

## Compose with Raylib (app layer)

```csharp
services.AddRayTracing().UseCpuBackend();
services.AddSingleton<IFramePresenter, RaylibCpuFramePresenter>();

var backend = provider.GetRequiredService<IRayTracingBackend>();
var presenter = provider.GetRequiredService<IFramePresenter>();
await backend.UploadSceneAsync(SceneCompiler.Compile(scene));
await backend.RenderAsync(camera, sampleIndex);
if (backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h))
    presenter.PresentCpuFrame(pixels, w, h);
```

Dogfood sample: [novolis-dogfooding/apps/RaytraceHello](../novolis-dogfooding/apps/RaytraceHello).

## Policy

- No reference to `Novolis.Raylib.*` or `Novolis.Simulation.*` from platform Rendering packages.
- `novolis-raylib` may reference only `Novolis.Rendering.Presentation.Abstractions` — never Scene/Materials/Compile.

Repository: [github.com/Novolis-Platform/novolis-rendering](https://github.com/Novolis-Platform/novolis-rendering)
