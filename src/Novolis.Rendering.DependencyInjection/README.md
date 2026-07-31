# Novolis.Rendering.DependencyInjection

Microsoft.Extensions.DependencyInjection registration for scene compilation and ray tracing backends (CPU, ILGPU, Vulkan).

## Install

```bash
dotnet add package Novolis.Rendering.DependencyInjection
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Microsoft.Extensions.DependencyInjection`.

## Quick start — explicit backend

```csharp
using Novolis.Rendering.DependencyInjection;

services.AddRayTracing()
    .UseCpuBackend(deterministic: true);

var compiler = provider.GetRequiredService<SceneCompilerService>();
var backend = provider.GetRequiredService<IRayTracingBackend>();
var compiled = compiler.Compile(authoringScene);
```

## Quick start — environment backend

```csharp
services.AddRayTracingFromEnvironment();
// NOVOLIS_RAY_BACKEND=cpu|vulkan (default ilgpu)
```

Chain `UseIlgpuBackend()` or `UseVulkanBackend()` instead of `UseCpuBackend()` when registering explicitly.

## API

| Type | Role |
|------|------|
| `RenderingServiceCollectionExtensions` | `AddRayTracing`, `UseCpuBackend`, `UseIlgpuBackend`, `UseVulkanBackend`, `AddRayTracingFromEnvironment` |
| `SceneCompilerService` | DI wrapper for `SceneCompiler.Compile` |

Register a presenter separately (`RaylibCpuFramePresenter`, `SilkOpenGlFramePresenter`); DI does not add window hosts.

Only one `IRayTracingBackend` registration should be active per service collection.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Backends.Cpu` | CPU backend implementation |
| `Novolis.Rendering.Backends.Igpu` | ILGPU backend |
| `Novolis.Rendering.Backends.Vulkan` | Vulkan backend |
| `Novolis.Rendering.Compile` | Static scene compiler used by `SceneCompilerService` |
| `Novolis.Rendering.Runtime` | `IRayTracingBackend`, `CompiledScene` |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Materials and backends](../../docs/materials-and-backends.md)
