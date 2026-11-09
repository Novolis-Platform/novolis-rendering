<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-rendering/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-rendering/) · [Source](https://github.com/Novolis-Platform/novolis-rendering)
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.Backends.Vulkan

`IRayTracingBackend` **contract shim** labeled for lab backend cycling. Tracing is CPU (`CpuRayTracingBackend`). Rendering cannot take Silk.NET (`NOV2016`); real Vulkan/Shaderc lives in `Novolis.Silk.Compute` and is composed only at the GAME layer.

## Install

```bash
dotnet add package Novolis.Rendering.Backends.Vulkan
```

Prefer `Novolis.Rendering.Backends.Cpu` or `.Igpu` for real path tracing.

## Quick start

```csharp
using Novolis.Rendering.Backends.Vulkan;
using Novolis.Rendering.Runtime;

using var backend = new VulkanRayTracingBackend(deterministic: true);
await backend.ResizeAsync(32, 24);
await backend.UploadSceneAsync(compiledScene);
await backend.RenderAsync(camera, sampleIndex: 0);
backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
// BackendLabel == "Vulkan (CPU shim — Silk.Compute owns real VK)"
```

Select with `NOVOLIS_RAY_BACKEND=vulkan`, `PathTraceBackendKind.Vulkan`, or `UseVulkanBackend()` in DI.

## API

| Type | Role |
|------|------|
| `VulkanRayTracingBackend` | `IRayTracingBackend` that delegates to CPU |

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Backends.Cpu` | Reference progressive path tracer |
| `Novolis.Rendering.Backends.Igpu` | ILGPU kernels |
| `Novolis.Silk.Compute` | GLSL → SPIR-V (Silk island) |
| `docs/roadmap-raytracing.md` | v1 exit notes |
