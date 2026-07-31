# Novolis.Rendering.Compile

Compiles authoring `Scene` graphs into flat `CompiledScene` structures: world-space triangles, deduplicated `GpuMaterial` tables, lights, and a binary BVH for ray tracing backends.

## Install

```bash
dotnet add package Novolis.Rendering.Compile
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Rendering.Scene`, `Novolis.Rendering.Materials`, `Novolis.Rendering.Runtime`.

## Quick start

```csharp
using Novolis.Rendering.Compile;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;

var authoring = new SceneBuilder()
    .AddGround(MaterialPresets.Standard(MaterialPresets.Colors.White, roughness: 0.9f))
    .AddBox(Vector3.Zero, new Vector3(0.25f), MaterialPresets.Metal(MaterialPresets.Colors.Silver))
    .AddDirectionalLight(new Vector3(-0.4f, -1f, -0.3f), Vector3.One)
    .Build();

var compiled = SceneCompiler.Compile(authoring);
// compiled.Triangles, .Materials, .Lights, .BvhNodes, .TriangleOrder, .BvhRootIndex
```

Each mesh instance is transformed, triangulated, and assigned a material index. Materials are compiled via `MaterialCompiler` and deduplicated by `IMaterial` reference. Empty scenes return `CompiledScene.Empty` with lights/materials preserved when applicable.

## API

| Type | Role |
|------|------|
| `SceneCompiler` | Static `Compile(Scene)` → `CompiledScene` |

Output types live in `Novolis.Rendering.Runtime`: `CompiledScene`, `GpuTriangle`, `GpuMaterial`, `GpuLight`, `TriangleBvhNode`.

## Support

Pre-release platform library. BVH build is internal; only `SceneCompiler.Compile` is public.

Transforms mesh instances and deduplicates materials by reference equality.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Scene` | Authoring `Scene`, `SceneBuilder`, `MeshInstance`, lights |
| `Novolis.Rendering.Materials` | `IMaterial` models compiled during scene build |
| `Novolis.Rendering.Runtime` | Flat runtime scene consumed by backends |
| `Novolis.Rendering.DependencyInjection` | `SceneCompilerService` DI wrapper |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Materials and backends](../../docs/materials-and-backends.md)
- [Roadmap: ray tracing](../../docs/roadmap-raytracing.md)
