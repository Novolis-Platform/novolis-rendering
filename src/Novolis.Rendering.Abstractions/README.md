# Novolis.Rendering.Abstractions

Legacy bootstrap types and a host-neutral CPU RGBA framebuffer. New code should prefer `Novolis.Rendering.Scene` for scenes and `Novolis.Rendering.Presentation.Abstractions` for output contracts.

## Install

```bash
dotnet add package Novolis.Rendering.Abstractions
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Math.Geometry`.

## Quick start

```csharp
using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Runtime;

var buffer = new ImageBuffer(320, 240);
buffer.Clear(Rgba32.Black);

var camera = CameraSnapshot.LookAt(
    position: new(2f, 1f, 3f),
    target: Vector3.Zero,
    up: Vector3.UnitY,
    verticalFovDegrees: 60f,
    aspectRatio: 320f / 240f);
```

`ImageBuffer` provides dense row-major `Rgba32[]` storage with `AsSpan()`, `Clear`, and indexed pixel access. CPU backends write into `ImageBufferRenderOutput` which wraps this type.

## API

| Type | Role |
|------|------|
| `ImageBuffer` | CPU RGBA framebuffer (width, height, pixels) |

`CameraSnapshot` and ray tracing contracts moved to `Novolis.Rendering.Runtime` and `Novolis.Rendering.Presentation.Abstractions`.

## Support

Pre-release platform library. Obsolete scene types remain for migration only; prefer `Novolis.Rendering.Scene`.

Depends on `Novolis.Math.Geometry` for `Rgba32` and buffer types.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Runtime` | `CameraSnapshot`, `CompiledScene`, `IRayTracingBackend` |
| `Novolis.Rendering.Scene` | Authoring scenes (replaces obsolete `RenderScene`) |
| `Novolis.Rendering.Presentation.Abstractions` | `IRenderOutput`, `IFramePresenter` |
| `Novolis.Rendering` | Umbrella meta-package for the full stack |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Design](../../docs/design.md)
- [Materials and backends](../../docs/materials-and-backends.md)
