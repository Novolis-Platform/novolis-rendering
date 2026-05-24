<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Rendering` | `dotnet add package Novolis.Rendering` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering/README.md) |
| `Novolis.Rendering.Abstractions` | `dotnet add package Novolis.Rendering.Abstractions` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Abstractions/README.md) |
| `Novolis.Rendering.Backends.Cpu` | `dotnet add package Novolis.Rendering.Backends.Cpu` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Backends.Cpu/README.md) |
| `Novolis.Rendering.Backends.Igpu` | `dotnet add package Novolis.Rendering.Backends.Igpu` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Backends.Igpu/README.md) |
| `Novolis.Rendering.Backends.Vulkan` | `dotnet add package Novolis.Rendering.Backends.Vulkan` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Backends.Vulkan/README.md) |
| `Novolis.Rendering.Compile` | `dotnet add package Novolis.Rendering.Compile` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Compile/README.md) |
| `Novolis.Rendering.DependencyInjection` | `dotnet add package Novolis.Rendering.DependencyInjection` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.DependencyInjection/README.md) |
| `Novolis.Rendering.Materials` | `dotnet add package Novolis.Rendering.Materials` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Materials/README.md) |
| `Novolis.Rendering.PathTrace.Demos` | `dotnet add package Novolis.Rendering.PathTrace.Demos` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.PathTrace.Demos/README.md) |
| `Novolis.Rendering.Presentation.Abstractions` | `dotnet add package Novolis.Rendering.Presentation.Abstractions` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Presentation.Abstractions/README.md) |
| `Novolis.Rendering.Presentation.Raylib` | `dotnet add package Novolis.Rendering.Presentation.Raylib` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Presentation.Raylib/README.md) |
| `Novolis.Rendering.Presentation.Silk` | `dotnet add package Novolis.Rendering.Presentation.Silk` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Presentation.Silk/README.md) |
| `Novolis.Rendering.Runtime` | `dotnet add package Novolis.Rendering.Runtime` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Runtime/README.md) |
| `Novolis.Rendering.Scene` | `dotnet add package Novolis.Rendering.Scene` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Scene/README.md) |
| `Novolis.Rendering.Testing` | `dotnet add package Novolis.Rendering.Testing` | [README](https://github.com/Novolis-Platform/novolis-rendering/blob/main/src/Novolis.Rendering.Testing/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->

# novolis-rendering

**Graphics-host-neutral ray tracing** — authoring, compilation, CPU/GPU backends, and framebuffer contracts. Computes RGBA frames; **does not** own windows, GPU draw calls, or input.

Display adapters (`Novolis.Rendering.Presentation.Raylib`, `Novolis.Rendering.Presentation.Silk`, …) live in this repo and reference host runtimes only. See [library boundaries](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md).

## Pipeline

```text
Scene + IMaterial  →  SceneCompiler  →  CompiledScene
  →  IRayTracingBackend  →  IRenderOutput (CPU pixels)
  →  IFramePresenter (Presentation.Raylib / Presentation.Silk)
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
| `Novolis.Rendering.Backends.Igpu` | ILGPU GPU path tracer (CPU fallback when no GPU or `deterministic: true`) |
| `Novolis.Rendering.Backends.Vulkan` | Vulkan compute path tracing (SPIR-V) |
| `Novolis.Rendering.DependencyInjection` | `AddRayTracing()`, `UseCpuBackend()` |
| `Novolis.Rendering.Presentation.Silk` | Silk.NET window + OpenGL CPU presenter |
| `Novolis.Rendering.Presentation.Raylib` | Raylib CPU frame presenter |
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
services.AddRayTracing().UseIlgpuBackend();
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

- No reference to `Novolis.Raylib.*` or `Novolis.Simulation.*` from core rendering packages (Scene, Compile, backends).
- `Novolis.Rendering.Presentation.Raylib` is the **only** rendering package that references `Novolis.Raylib.Runtime` (texture blit only).
- `novolis-raylib` is a pure Raylib host — **no** `Novolis.Rendering.*` references.

Repository: [github.com/Novolis-Platform/novolis-rendering](https://github.com/Novolis-Platform/novolis-rendering)

