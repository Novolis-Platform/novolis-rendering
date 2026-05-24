# Novolis.Rendering.Abstractions

Graphics-host-neutral frame buffers, legacy scene meshes, and ray tracing contracts.

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
var camera = CameraSnapshot.LookAt(
    position: new(2f, 1f, 3f),
    target: Vector3.Zero,
    up: Vector3.UnitY,
    verticalFovDegrees: 60f,
    aspectRatio: 320f / 240f);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Runtime` | `CameraSnapshot`, `CompiledScene`, `IRayTracingBackend` |
| `Novolis.Rendering.Scene` | Authoring scenes (replaces obsolete `RenderScene`) |
| `Novolis.Rendering` | Umbrella meta-package for the full stack |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Design](../../docs/design.md)
- [Materials and backends](../../docs/materials-and-backends.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
