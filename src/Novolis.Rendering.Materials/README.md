# Novolis.Rendering.Materials

Authoring materials (standard, glass, skin, emissive) and compilation to `GpuMaterial`.

## Install

```bash
dotnet add package Novolis.Rendering.Materials
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.Materials;

var metal = MaterialPresets.Metal(MaterialPresets.Colors.Silver, roughness: 0.08f);
var gpu = MaterialCompiler.Compile(metal);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Runtime` | `GpuMaterial` and `MaterialModel` |
| `Novolis.Rendering.Scene` | Attach materials to mesh instances |

## More documentation

- [Materials and backends](../../docs/materials-and-backends.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
