# Novolis.Rendering.Testing

Golden framebuffer hashing helpers for headless render tests (no native window).

## Install

```bash
dotnet add package Novolis.Rendering.Testing
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.Testing;

var hash = FramebufferGoldenAssert.Sha256Hex(backend.Output);
Assert.Equal(expectedHash, hash);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Backends.Cpu` | Deterministic CPU backend for goldens |
| `Novolis.Rendering.Presentation.Abstractions` | `IRenderOutput` input |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
