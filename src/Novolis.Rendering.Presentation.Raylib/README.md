# Novolis.Rendering.Presentation.Raylib

Raylib texture presenter for uploading CPU RGBA frames to a window.

## Install

```bash
dotnet add package Novolis.Rendering.Presentation.Raylib
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Raylib` packages.

## Quick start

```csharp
using Novolis.Rendering.Presentation.Raylib;

using var presenter = new RaylibCpuFramePresenter();
presenter.PresentCpuFrame(pixels, width, height);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Presentation.Silk` | Silk.NET host without Raylib |
| `Novolis.Raylib` | Low-level Raylib bindings |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
