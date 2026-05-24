# Novolis.Rendering.DependencyInjection

Microsoft.Extensions.DependencyInjection registration for scene compilation and ray tracing backends.

## Install

```bash
dotnet add package Novolis.Rendering.DependencyInjection
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.DependencyInjection;

services.AddRayTracing()
    .UseCpuBackend(deterministic: true);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Backends.Cpu` | CPU backend implementation |
| `Novolis.Rendering.Backends.Vulkan` | `UseVulkanBackend()` |
| `Novolis.Rendering.Backends.Igpu` | `UseIlgpuBackend()` |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
