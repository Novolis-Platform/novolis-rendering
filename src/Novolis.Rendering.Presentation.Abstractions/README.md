# Novolis.Rendering.Presentation.Abstractions

Host-neutral presentation contracts: CPU/GPU outputs and frame presenters.

## Install

```bash
dotnet add package Novolis.Rendering.Presentation.Abstractions
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.Presentation.Abstractions;

IRenderOutput output = backend.Output;
if (output.TryGetCpuPixels(out var pixels, out var w, out var h))
    presenter.PresentCpuFrame(pixels, w, h);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Presentation.Silk` | Silk.NET OpenGL window loop |
| `Novolis.Rendering.Presentation.Raylib` | Raylib texture presenter |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
