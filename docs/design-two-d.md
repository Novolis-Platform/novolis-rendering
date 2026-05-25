# 2D rendering (`Novolis.Rendering.TwoD`)

Orthographic 2D renderer for platformers (Mario-style): backgrounds, sprite animation, static polygons with colliders, HUD, and menus.

## Packages

| Package | Role |
|---------|------|
| `Novolis.Rendering.TwoD` | Host-neutral scene, collision, HUD, menus |
| `Novolis.Rendering.Backends.TwoD.Silk` | Silk.NET OpenGL draw + window loop + PNG load |

Path tracing packages (`Novolis.Rendering.Runtime`, `Backends.Cpu`, etc.) stay separate. Apps can use both stacks if needed.

## Conventions

- World space: BCL `Vector3` on the **XZ plane** (Y = 0).
- Screen HUD/menus: pixel coordinates, origin top-left.
- No `Vector2` in public API.

## Mario-style checklist

| Feature | API |
|---------|-----|
| Background image | `TwoDScenePrimitives.AddBackground` + `SilkTwoDPngLoader` |
| Character animation | `TwoDAnimatedSprite` + `TwoDAnimationClip.FromRow` |
| Platforms / blocks | `TwoDScene.AddPlatform` or `TwoDStaticPolygon` |
| Collision | `TwoDCollisionWorld.MoveCircle` |
| HUD | `TwoDHud.AddText` / `AddSprite` |
| Menus | `TwoDMenuStack.Push` + `SilkTwoDGame` menu keys |

## Layered grid export

`TwoDSceneGridRasterizer` / `TwoDScene.ToLayeredGrids()` produce per-`TwoDDrawLayer` `DenseGrid<Rgba32>` buffers for unit tests and ASCII debug (`TwoDLayerGridSet.ToAscii`). No Silk/OpenGL required.

## Related

- [design.md](design.md) — path tracing stack
- [gameengine-2d-scene-rendering.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/imports-todo/gameengine-2d-scene-rendering.md) — historical Raylib placement note
