# Novolis.Rendering.Materials

Authoring materials for ray tracing: standard PBR, glass, skin, and emissive models with presets and compilation to blittable `GpuMaterial` records.

## Install

```bash
dotnet add package Novolis.Rendering.Materials
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Rendering.Runtime`.

## Quick start — presets

```csharp
using Novolis.Rendering.Materials;

var metal = MaterialPresets.Metal(MaterialPresets.Colors.Silver, roughness: 0.08f);
var glass = MaterialPresets.Glass(tint: new Vector3(0.75f, 0.9f, 1f), roughness: 0.02f, ior: 1.45f);
var gpu = MaterialCompiler.Compile(metal);
```

## Quick start — custom records

```csharp
IMaterial skin = new SkinMaterial
{
    BaseColor = new Vector3(0.85f, 0.65f, 0.55f),
    Roughness = 0.45f,
    SubsurfaceStrength = 0.5f,
};

IMaterial glow = MaterialPresets.Emissive(new Vector3(1f, 0.55f, 0.2f), strength: 4f);
```

Attach materials to meshes via `SceneBuilder.AddBox(..., material)` or `MeshInstance`.

## API

| Type | Role |
|------|------|
| `IMaterial` | Marker for authoring material models |
| `StandardMaterial` | PBR dielectric/metal (base color, roughness, metallic, emission) |
| `GlassMaterial` | Transmission (tint, roughness, IOR, absorption) |
| `SkinMaterial` | Approximate subsurface scattering |
| `EmissiveMaterial` | Geometry-driven light emission |
| `MaterialPresets` | `Standard`, `Metal`, `Glass`, `Skin`, `Emissive`, `Colors.*` |
| `MaterialCompiler` | `Compile(IMaterial)` → `GpuMaterial` |

Compiled output uses `MaterialModel` (`Standard`, `Glass`, `Skin`, `Emissive`) and three `Vector4` parameter slots defined in `Novolis.Rendering.Runtime`.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Runtime` | `GpuMaterial`, `MaterialModel` |
| `Novolis.Rendering.Scene` | Attach materials to mesh instances |
| `Novolis.Rendering.Compile` | Deduplicates and compiles materials during scene build |

## More documentation

- [Materials and backends](../../docs/materials-and-backends.md)
