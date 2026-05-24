# Novolis.Rendering.Compile

Compiles authoring `Scene` graphs into flat `CompiledScene` structures (triangles, materials, BVH).

## Install

```bash
dotnet add package Novolis.Rendering.Compile
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.Compile;

var compiled = SceneCompiler.Compile(authoringScene);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Scene` | Authoring scene input |
| `Novolis.Rendering.Runtime` | `CompiledScene` output type |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
