# Novolis.Rendering.Backends.Vulkan

Vulkan compute path tracing (SPIR-V kernels) with CPU fallback when Vulkan is unavailable.

## Install

```bash
dotnet add package Novolis.Rendering.Backends.Vulkan
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Vulkan 1.2+ runtime.

## Quick start

```csharp
using Novolis.Rendering.Backends.Vulkan;

await using var backend = new VulkanRayTracingBackend();
await backend.ResizeAsync(1280, 720);
await backend.UploadSceneAsync(compiledScene);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Backends.Igpu` | ILGPU instead of Vulkan |
| `Novolis.Rendering.Presentation.Abstractions` | `ICpuBackedGpuSurface` from GPU output |

## More documentation

- [Roadmap: ray tracing](../../docs/roadmap-raytracing.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
