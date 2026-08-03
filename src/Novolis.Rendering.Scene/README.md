<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-rendering">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.Scene

Authoring scene graph for ray tracing: mesh instances with transforms, lights, and fluent `SceneBuilder` helpers for common primitives.

## Install

```bash
dotnet add package Novolis.Rendering.Scene
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Rendering.Materials`.

## Quick start

```csharp
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;

var scene = new SceneBuilder()
    .AddGround(MaterialPresets.Standard(MaterialPresets.Colors.White, roughness: 0.9f))
    .AddBox(Vector3.Zero, new Vector3(0.25f), MaterialPresets.Metal(MaterialPresets.Colors.Silver))
    .AddDirectionalLight(new Vector3(-0.4f, -1f, -0.3f), Vector3.One, intensity: 1f)
    .Build();
```

`AddMesh(vertices, indices, material, transform)` accepts arbitrary triangle meshes. `AddBox(center, halfExtents, material)` and `AddGround(material, size)` cover common test geometry.

## Quick start — compile and trace

```csharp
using Novolis.Rendering.Compile;

var compiled = SceneCompiler.Compile(scene);
await backend.UploadSceneAsync(compiled);
```

## API

| Type | Role |
|------|------|
| `Scene` | Authoring root with `Meshes` and `Lights` collections |
| `SceneBuilder` | Fluent `AddMesh`, `AddGround`, `AddBox`, `AddDirectionalLight`, `Build` |
| `MeshInstance` | Vertices, triangle indices, material, transform |
| `LightDefinition` | Directional/point light with color and intensity |
| `LightKind` | `Directional`, `Point` |

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Materials` | `IMaterial` models attached to meshes |
| `Novolis.Rendering.Compile` | Compile to `CompiledScene` |
| `Novolis.Rendering.PathTrace.Demos` | `ShowcaseScenes` built with `SceneBuilder` |

## More documentation

- [Getting started](../../docs/getting-started.md)

