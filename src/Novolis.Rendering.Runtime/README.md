# Novolis.Rendering.Runtime

Flat runtime scene data and the `IRayTracingBackend` contract shared by CPU, ILGPU, and Vulkan backends.

## Install

```bash
dotnet add package Novolis.Rendering.Runtime
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Runtime;

IRayTracingBackend backend = new CpuRayTracingBackend();
await backend.ResizeAsync(640, 480);
await backend.UploadSceneAsync(compiledScene);
await backend.RenderAsync(camera, sampleIndex: 0);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Compile` | Build `CompiledScene` from authoring `Scene` |
| `Novolis.Rendering.Backends.Cpu` | Reference CPU path tracer |
| `Novolis.Rendering.Presentation.Abstractions` | `IRenderOutput` for presenters |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Roadmap: ray tracing](../../docs/roadmap-raytracing.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
