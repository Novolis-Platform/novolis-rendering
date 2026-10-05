<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-rendering/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-rendering/) · [Source](https://github.com/Novolis-Platform/novolis-rendering)
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.Appearance

Declarative appearance stacks. Geometry starts grey. Layers are the asset. Hosts compile stacks to CPU pixels today; GLSL / `GpuMaterial` compilers can consume the same graph later.

Domains stay separate: **surface**, **volume**, **light**, **effect**, **post**. Compose them in the app. A stack never learns how to spawn brass or draw a window.

## Install

```bash
dotnet add package Novolis.Rendering.Appearance
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.Math.Geometry`.

## Quick start

```csharp
using Novolis.Rendering.Appearance;

var wall = AppearanceRecipes.FilthyGlowWall(seed: 69);
var pixels = AppearanceBaker.BakeSurface(wall, 128, 128, meters: 2f, seed: 69);
```

A filthy glowing wall is still just parameters:

```text
Grey surface
→ steel base
→ panel grid
→ noise / grime
→ edge wear
→ green emission (noise mask)
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Materials` | Path-trace `IMaterial` → `GpuMaterial` |
| `Novolis.Rendering.TwoD` | Register baked pixels as planar textures |
| `Novolis.Silk.Capture` | Framebuffer readback for post stacks |
