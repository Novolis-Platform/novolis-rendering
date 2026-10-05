<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-rendering/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-rendering/) · [Source](https://github.com/Novolis-Platform/novolis-rendering)
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.Presentation.Raylib

Raylib texture presenter that uploads CPU RGBA frames from ray tracing backends and draws them full-screen. The only rendering package that references `Novolis.Raylib`.

## Install

```bash
dotnet add package Novolis.Rendering.Presentation.Raylib
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Raylib.Runtime`, `Novolis.Rendering.Presentation.Abstractions`.

## Quick start

```csharp
using Novolis.Rendering.Presentation.Raylib;

using var presenter = new RaylibCpuFramePresenter();

// After backend.RenderAsync(...) and TryGetCpuPixels:
if (backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h))
    presenter.PresentCpuFrame(pixels, w, h);
```

The presenter recreates the Raylib texture when dimensions change, flips rows for OpenGL-style origin, and draws at `(0, 0)`. Call `Dispose()` to unload the texture.

## Quick start — with PathTrace.Demos display buffer

```csharp
var display = new PathTraceDisplayBuffer();
var lastGen = -1;
display.TryPresent(presenter, ref lastGen);
```

## API

| Type | Role |
|------|------|
| `RaylibCpuFramePresenter` | `IFramePresenter` + `IDisposable`; uploads and blits CPU RGBA |

## Dogfooding / apps

Pairs with `Novolis.Rendering.PathTrace.Demos` in Raylib path-tracing samples. Raylib owns the window and input loop; this package only blits finished frames.

## Support

Pre-release platform library. This is the only `Novolis.Rendering.*` package that references `Novolis.Raylib`.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Presentation.Abstractions` | `IFramePresenter`, `IRenderOutput` |
| `Novolis.Silk` | GLFW/OpenGL presenter (no Raylib; `novolis-silk`) |
| `Novolis.Rendering.PathTrace.Demos` | Shared scenes, workers, display buffer |
| `Novolis.Raylib` | Low-level Raylib bindings and texture helpers |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Materials and backends](../../docs/materials-and-backends.md)

