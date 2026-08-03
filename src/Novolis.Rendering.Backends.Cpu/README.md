<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-rendering">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.Backends.Cpu

CPU path tracing backend with progressive accumulation and optional deterministic sampling for golden tests.

## Install

```bash
dotnet add package Novolis.Rendering.Backends.Cpu
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Rendering.Runtime`.

## Quick start

```csharp
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Runtime;

var backend = new CpuRayTracingBackend(deterministic: true);
await backend.ResizeAsync(320, 240);
await backend.UploadSceneAsync(DemoSceneFactory.UnitCubeRoom());

var camera = CameraSnapshot.LookAt(
    new Vector3(1.2f, 0.8f, 2f), new Vector3(0f, 0.25f, 0f), Vector3.UnitY, 60f, 320f / 240f);

await backend.RenderAsync(camera, sampleIndex: 0);
backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
```

`ResetAccumulation()` clears integrated samples. `SampleCount` tracks progressive rendering progress. `BackendLabel` is `"CPU"` or `"CPU (deterministic)"`.

## API

| Type | Role |
|------|------|
| `CpuRayTracingBackend` | `IRayTracingBackend`; CPU-only output, no GPU surface |
| `DemoSceneFactory` | `UnitCubeRoom()` → precompiled demo `CompiledScene` |

## Dogfooding / apps

Reference backend for correctness checks and `Novolis.Rendering.Testing` golden hashes. Selected via `NOVOLIS_RAY_BACKEND=cpu` or `PathTraceBackendKind.Cpu`.

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).

No GPU driver or native window required.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Backends.Igpu` | GPU compute via ILGPU |
| `Novolis.Rendering.Backends.Vulkan` | Vulkan compute shaders |
| `Novolis.Rendering.Testing` | SHA-256 golden assertions |
| `Novolis.Rendering.DependencyInjection` | `UseCpuBackend(deterministic:)` |

## More documentation

- [Materials and backends](../../docs/materials-and-backends.md)
- [Getting started](../../docs/getting-started.md)

