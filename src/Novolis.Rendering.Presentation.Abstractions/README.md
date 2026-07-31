# Novolis.Rendering.Presentation.Abstractions

Host-neutral presentation contracts between ray tracing backends and window hosts: CPU pixel output, optional GPU surfaces, and frame presenters.

## Install

```bash
dotnet add package Novolis.Rendering.Presentation.Abstractions
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Math.Geometry`.

## Quick start — CPU blit path

```csharp
using Novolis.Rendering.Presentation.Abstractions;

IRenderOutput output = backend.Output;
if (output.TryGetCpuPixels(out var pixels, out var w, out var h))
    presenter.PresentCpuFrame(pixels, w, h);
```

## Quick start — buffer wrapper

```csharp
var renderOutput = new ImageBufferRenderOutput { Buffer = new ImageBuffer(640, 480) };
```

Implement `IFramePresenter` in Raylib or Silk packages; implement `IGpuFramePresenter` when importing native GPU handles.

## API

| Type | Role |
|------|------|
| `IRenderOutput` | `TryGetCpuPixels` from a backend |
| `IFramePresenter` | `PresentCpuFrame` to a host surface |
| `IRenderGpuSurface` | Opaque GPU handle + dimensions |
| `ICpuBackedGpuSurface` | GPU surface readable via CPU staging copy |
| `IGpuFramePresenter` | `PresentGpuFrame` for native handles |
| `ImageBufferRenderOutput` | `IRenderOutput` over `ImageBuffer` |
| `Key` / `MouseButton` | Shared input enums for Silk hosts |

Backends implement `IRenderOutput`; presenters consume CPU pixels or GPU surfaces without referencing specific backends.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Presentation.Silk` | Silk.NET OpenGL window loop and presenters |
| `Novolis.Rendering.Presentation.Raylib` | Raylib texture presenter |
| `Novolis.Rendering.Runtime` | `IRayTracingBackend.Output` |
| `Novolis.Rendering.Abstractions` | `ImageBuffer` pixel storage |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Design](../../docs/design.md)
