# Novolis.Rendering.Backends.Igpu

ILGPU compute path tracing with automatic CPU fallback when no suitable GPU is present.

## Install

```bash
dotnet add package Novolis.Rendering.Backends.Igpu
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), CUDA/OpenCL-capable GPU optional.

## Quick start

```csharp
using Novolis.Rendering.Backends.Igpu;

await using var backend = new IlgpuRayTracingBackend();
await backend.ResizeAsync(1280, 720);
await backend.UploadSceneAsync(compiledScene);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Backends.Cpu` | Pure CPU reference and deterministic tests |
| `Novolis.Rendering.DependencyInjection` | `UseIlgpuBackend()` registration |

## More documentation

- [Roadmap: ray tracing](../../docs/roadmap-raytracing.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
