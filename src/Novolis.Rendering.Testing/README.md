<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-rendering">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.Testing

Golden framebuffer hashing helpers for headless render tests — SHA-256 digests over raw RGBA bytes with no native window required.

## Install

```bash
dotnet add package Novolis.Rendering.Testing
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Rendering.Presentation.Abstractions`.

## Quick start — pixel span

```csharp
using Novolis.Rendering.Testing;

backend.Output.TryGetCpuPixels(out var pixels, out _, out _);
var hash = FramebufferGoldenAssert.Sha256Hex(pixels);
Assert.Equal(expectedHash, hash);
```

## Quick start — render output

```csharp
var hash = FramebufferGoldenAssert.Sha256Hex(backend.Output);
```

Use `CpuRayTracingBackend(deterministic: true)` from `Novolis.Rendering.Backends.Cpu` for stable golden values across runs.

Commit expected digests as lowercase hex strings; update intentionally when shading or sampling changes.

## API

| Type | Role |
|------|------|
| `FramebufferGoldenAssert` | `Sha256Hex(ReadOnlySpan<Rgba32>)`, `Sha256Hex(IRenderOutput)` |

Returns lowercase hex SHA-256 over row-major RGBA bytes (R, G, B, A per pixel). Throws `InvalidOperationException` when `IRenderOutput` has no CPU pixels.

## Dogfooding / apps

Used by backend unit tests (`Novolis.Rendering.Backends.Cpu.Tests`) for regression checks without GPU drivers or window hosts.

## Support

Pre-release platform library. Store expected digests in test data; re-run with `deterministic: true` when updating goldens.

Works with any backend that exposes CPU pixels via `IRenderOutput`.

Pair with `DemoSceneFactory.UnitCubeRoom()` for a minimal golden scene.

No image decoding — hashes raw RGBA bytes only.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Backends.Cpu` | Deterministic CPU backend for goldens |
| `Novolis.Rendering.Presentation.Abstractions` | `IRenderOutput`, `ImageBufferRenderOutput` |
| `Novolis.Rendering.Compile` | Build scenes under test |

## More documentation

- [Getting started](../../docs/getting-started.md)

