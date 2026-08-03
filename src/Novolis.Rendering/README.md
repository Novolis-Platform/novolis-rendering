<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-rendering">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering

Meta-package composing the core ray tracing stack: abstractions, scene, materials, compile, CPU backend, and dependency injection. GPU backends and window presenters are separate packages.

## Install

```bash
dotnet add package Novolis.Rendering
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

Includes: `Novolis.Rendering.Abstractions`, `.Scene`, `.Materials`, `.Compile`, `.Runtime`, `.Backends.Cpu`, `.DependencyInjection`, `.Presentation.Abstractions`.

Does **not** include ILGPU, Vulkan, Silk/Raylib presenters, 2D stack, or path-trace demos — add those explicitly.

## Quick start

```csharp
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;
using Novolis.Rendering.Runtime;

var authoring = new SceneBuilder()
    .AddGround(MaterialPresets.Standard(MaterialPresets.Colors.White, 0.9f))
    .AddBox(Vector3.Zero, new Vector3(0.25f), MaterialPresets.Metal(MaterialPresets.Colors.Silver))
    .AddDirectionalLight(new Vector3(-0.4f, -1f, -0.3f), Vector3.One)
    .Build();

var compiled = SceneCompiler.Compile(authoring);
IRayTracingBackend backend = new CpuRayTracingBackend();
```

## Quick start — DI

```csharp
using Novolis.Rendering.DependencyInjection;

services.AddRayTracing().UseCpuBackend();
```

## API

This package has no unique public types. See included packages:

| Package | Role |
|---------|------|
| `Novolis.Rendering.Scene` | Authoring scenes |
| `Novolis.Rendering.Compile` | `SceneCompiler` |
| `Novolis.Rendering.Runtime` | `IRayTracingBackend`, `CompiledScene` |
| `Novolis.Rendering.Backends.Cpu` | CPU path tracer (included) |
| `Novolis.Rendering.DependencyInjection` | `AddRayTracing()` extensions |

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Backends.Igpu` | GPU compute (not in meta-package) |
| `Novolis.Rendering.Backends.Vulkan` | Vulkan compute (not in meta-package) |
| `Novolis.Rendering.Presentation.Silk` | Windowed Silk demos |
| `Novolis.Rendering.Presentation.Raylib` | Raylib presenter |
| `Novolis.Rendering.PathTrace.Demos` | Shared demo scenes and workers |
| `Novolis.Rendering.TwoD` | Separate 2D platformer scene model |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Design](../../docs/design.md)
- [Release](../../docs/release.md)

