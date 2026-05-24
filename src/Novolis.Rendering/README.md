# Novolis.Rendering

Meta-package composing the rendering stack: abstractions, scene, materials, compile, CPU backend, and DI.

## Install

```bash
dotnet add package Novolis.Rendering
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Scene;
using Novolis.Rendering.Runtime;

var compiled = SceneCompiler.Compile(authoringScene);
IRayTracingBackend backend = new CpuRayTracingBackend();
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Backends.Igpu` | GPU compute (not included in meta-package) |
| `Novolis.Rendering.Backends.Vulkan` | Vulkan compute (not included) |
| `Novolis.Rendering.Presentation.Silk` | Windowed demos |
| `Novolis.Rendering.Presentation.Raylib` | Raylib presenter |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Design](../../docs/design.md)
- [Release](../../docs/release.md)

## Support

Pre-release platform library. Add backend and presentation packages explicitly for GPU or window hosts.
