# Novolis.Rendering.Backends.Cpu

CPU path tracing backend with progressive accumulation and deterministic test mode.

## Install

```bash
dotnet add package Novolis.Rendering.Backends.Cpu
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Runtime;

var backend = new CpuRayTracingBackend(deterministic: true);
await backend.ResizeAsync(320, 240);
await backend.UploadSceneAsync(DemoSceneFactory.UnitCubeRoom());
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Backends.Igpu` | GPU compute via ILGPU |
| `Novolis.Rendering.Backends.Vulkan` | Vulkan compute shaders |

## More documentation

- [Materials and backends](../../docs/materials-and-backends.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
