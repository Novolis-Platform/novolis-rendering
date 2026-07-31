# Novolis.Rendering.Backends.TwoD.Silk

Silk.NET OpenGL backend and game loop for `Novolis.Rendering.TwoD` — orthographic platformer rendering, PNG loading, HUD/menus, and optional framebuffer capture.

## Install

```bash
dotnet add package Novolis.Rendering.Backends.TwoD.Silk
```

Depends on **Silk.NET** (windowing, OpenGL, input) and `Novolis.Rendering.TwoD`. Requires OpenGL 3.3+ and [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start — game loop

```csharp
using Novolis.Rendering.Backends.TwoD.Silk;
using Novolis.Rendering.TwoD;

SilkTwoDGame.Run("Platformer", 800, 600, ctx =>
{
    var bg = SilkTwoDPngLoader.LoadPng(ctx.Scene.Textures, "assets/level1.png");
    TwoDScenePrimitives.AddBackground(ctx.Scene, bg, worldWidth: 50f, worldHeight: 15f);
}, ctx =>
{
    ctx.Scene.Update(ctx.DeltaSeconds);
});
```

`SilkTwoDGameContext` exposes `Scene`, `Renderer`, `DeltaSeconds`, keyboard/mouse input, and menu navigation helpers (`IsMenuUpPressed`, `IsMenuConfirmPressed`, …). Menu stack input (W/S, arrows, Enter, Escape) is handled automatically when a menu is active.

## Quick start — PNG and manual draw

```csharp
var texture = SilkTwoDPngLoader.LoadPng(scene.Textures, "hero.png");
// … populate scene …
renderer.Resize(width, height);
renderer.DrawScene(scene);
```

## Quick start — streaming capture

```csharp
using var capture = new SilkTwoDFrameCaptureSession(new CaptureStreamOptions
{
    CaptureEveryNFrames = 2,
    MaxBufferedFrames = 32,
});

SilkTwoDGame.Run("Capture", 800, 600, initialize: null, update: ctx => { }, renderOverlay: null, capture);
// Drain PNG frames from capture.TryRead(out var frame) on any thread.
```

## API

| Type | Role |
|------|------|
| `SilkTwoDGame` | GLFW window loop with initialize/update/overlay/capture hooks |
| `SilkTwoDGameContext` | Per-frame scene, renderer, input, timing |
| `SilkTwoDRenderer` | OpenGL implementation of `ITwoDRenderer` |
| `SilkTwoDPngLoader` | Load PNG files into `TwoDTextureRegistry` |
| `SilkTwoDFrameCaptureSession` | Post-draw framebuffer capture queue |
| `CaptureStreamOptions` | Frame interval and queue depth |
| `SilkTwoDCapturedFrame` | PNG bytes, dimensions, frame index, elapsed time |

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.TwoD` | Host-neutral 2D scene, collision, HUD, menus |
| `Novolis.Rendering.Presentation.Silk` | Silk window for path-tracing demos (separate stack) |
