# 2D rendering (`Novolis.Rendering.Planar`)

Orthographic 2D renderer for platformers (Mario-style): backgrounds, sprite animation, static polygons with colliders, HUD, and menus.

## Packages

| Package | Role |
|---------|------|
| `Novolis.Rendering.Planar` | Host-neutral scene, collision, HUD, menus, tessellate, PNG load |
| `Novolis.Silk` | GLFW/OpenGL submit of tessellated quads (`novolis-silk`) |

Path tracing packages (`Novolis.Rendering.Runtime`, `Backends.Cpu`, etc.) stay separate. Apps can use both stacks if needed.

## Conventions

- World space: BCL `Vector3` on the **XZ plane** (Y = 0).
- Screen HUD/menus: pixel coordinates, origin top-left.
- No `Vector2` in public API.

## Mario-style checklist

| Feature | API |
|---------|-----|
| Background image | `PlanarScenePrimitives.AddBackground` + `PlanarPngLoader` |
| Character animation | `PlanarAnimatedSprite` + `PlanarAnimationClip.FromRow` |
| Platforms / blocks | `PlanarScene.AddPlatform` or `PlanarStaticPolygon` |
| Collision | `PlanarCollisionWorld.MoveCircle` |
| HUD | `PlanarHud.AddText` / `AddSprite` |
| Menus | `PlanarMenuStack.Push` + `SilkGame` menu keys |

## Layered grid export

`PlanarSceneGridRasterizer` / `PlanarScene.ToLayeredGrids()` produce per-`PlanarDrawLayer` `DenseGrid<Rgba32>` buffers for unit tests and ASCII debug (`PlanarLayerGridSet.ToAscii`). No Silk/OpenGL required.

## Related

- [design.md](design.md) — path tracing stack
- [gameengine-2d-scene-rendering.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/imports-todo/gameengine-2d-scene-rendering.md) — historical Raylib placement note
