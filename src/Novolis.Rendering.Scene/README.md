# Novolis.Rendering.Scene

Authoring scene graph: meshes, transforms, lights, and `SceneBuilder` helpers.

## Install

```bash
dotnet add package Novolis.Rendering.Scene
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;

var scene = new SceneBuilder()
    .AddGround(MaterialPresets.Standard(MaterialPresets.Colors.White, 0.9f))
    .AddBox(Vector3.Zero, new Vector3(0.25f), MaterialPresets.Metal(MaterialPresets.Colors.Silver))
    .AddDirectionalLight(new Vector3(-0.4f, -1f, -0.3f), Vector3.One)
    .Build();
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Materials` | `IMaterial` models and presets |
| `Novolis.Rendering.Compile` | Compile to `CompiledScene` |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
