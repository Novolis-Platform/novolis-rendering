<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-rendering/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-rendering/) · [Source](https://github.com/Novolis-Platform/novolis-rendering)
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.TwoD

Host-neutral 2D scene model for orthographic platformers (Mario-style): backgrounds, sprite animation, static polygons with colliders, HUD, and menus.

## Install

```bash
dotnet add package Novolis.Rendering.TwoD
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
var scene = new TwoDScene();
var texture = scene.Textures.Register(pixels, width, height, "hero.png");
var sheet = TwoDScenePrimitives.CreateSpriteSheet(scene, texture, 32, 32);
var run = TwoDAnimationClip.FromRow(sheet, row: 0, startColumn: 0, count: 4);

scene.AnimatedSprites.Add(new TwoDAnimatedSprite { Clip = run, Transform = { Position = Vector3PlanarExtensions.Xz(4, 2) } });
scene.AddPlatform(0, 0, 20, 1, Rgba32.Chartreuse);

scene.Hud.AddText("MARIO 000000", screenX: 16, screenY: 16);
scene.Menus.Push(new TwoDMenuScreen("SUPER NOVOLIS", [
    new TwoDMenuItem("1 PLAYER", tag: "start"),
    new TwoDMenuItem("OPTIONS", tag: "options"),
]));
```

Draw by tessellating into Math and submitting through a Silk host:

```csharp
using Novolis.Rendering.TwoD;
using Novolis.Silk;

var scene = new TwoDScene();
SilkGame.Run("Platformer", 800, 600, frame =>
{
    scene.Update(frame.DeltaSeconds);
    frame.Submit(scene.Tessellate(frame.Width, frame.Height));
});
```

## Packages

| Package | Role |
|---------|------|
| `Novolis.Rendering.TwoD` | Scene, collision, HUD, menus, tessellate to `PlanarDrawList` |
| `Novolis.Silk` | GLFW / OpenGL host (`Submit` / `Blit`) — compose in apps and labs |

## In-memory layered grids (tests / debug)

Rasterize a scene to per-layer `DenseGrid<Rgba32>` without a GPU:

```csharp
var grids = scene.ToLayeredGrids(TwoDGridRasterOptions.WorldCells(
    cellSize: 1f,
    worldBounds: new TwoDWorldBounds(0f, 0f, 40f, 40f)));

var ascii = grids.ToAscii(TwoDDrawLayer.World);
var merged = grids.CompositedToAscii();
```

| Mode | Cell meaning |
|------|----------------|
| `WorldCells` | One cell per world XZ slab (`CellSize` world units) |
| `ScreenPixels` | One cell per framebuffer pixel |

Layers: `Background`, `World`, `Foreground`, `Hud`, `Menu`, plus `Composited` in draw order.

## Conventions

- World space uses BCL `Vector3` on the **XZ plane** (Y = 0).
- Screen-space HUD/menu uses pixel coordinates (origin top-left).
- Textures are registered in `TwoDTextureRegistry`; PNG loading is provided by the Silk backend.

