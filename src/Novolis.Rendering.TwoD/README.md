# Novolis.Rendering.TwoD

Host-neutral 2D scene model for orthographic platformers (Mario-style): backgrounds, sprite animation, static polygons with colliders, HUD, and menus.

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

Draw with a backend:

```csharp
using Novolis.Rendering.Backends.TwoD.Silk;

SilkTwoDGame.Run("Platformer", 800, 600, ctx =>
{
    ctx.Scene.Update(ctx.DeltaSeconds);
    ctx.Renderer.DrawScene(ctx.Scene);
});
```

## Packages

| Package | Role |
|---------|------|
| `Novolis.Rendering.TwoD` | Scene, collision, HUD, menus |
| `Novolis.Rendering.Backends.TwoD.Silk` | Silk.NET OpenGL renderer + game loop |

## Conventions

- World space uses BCL `Vector3` on the **XZ plane** (Y = 0).
- Screen-space HUD/menu uses pixel coordinates (origin top-left).
- Textures are registered in `TwoDTextureRegistry`; PNG loading is provided by the Silk backend.
