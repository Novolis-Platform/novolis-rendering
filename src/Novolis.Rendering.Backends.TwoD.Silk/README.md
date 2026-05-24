# Novolis.Rendering.Backends.TwoD.Silk

Silk.NET OpenGL backend for `Novolis.Rendering.TwoD`.

## Features

- Orthographic world rendering (XZ plane)
- Textured sprites and sprite-sheet animation
- Filled/outline static polygons
- Screen-space HUD (bitmap font + icon quads)
- Menu stack with keyboard navigation (W/S, arrows, Enter, Escape)
- PNG loading via `SilkTwoDPngLoader`

## Example

```csharp
SilkTwoDGame.Run("Mario-style", 800, 600, ctx =>
{
    var bg = SilkTwoDPngLoader.LoadPng(ctx.Scene.Textures, "assets/level1.png");
    TwoDScenePrimitives.AddBackground(ctx.Scene, bg, worldWidth: 50f, worldHeight: 15f);
}, ctx =>
{
    ctx.Scene.Update(ctx.DeltaSeconds);
});
```
