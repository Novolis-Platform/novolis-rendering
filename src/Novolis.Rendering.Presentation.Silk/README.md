<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-rendering">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.Presentation.Silk

Silk.NET window loop and OpenGL presenters for path-tracing demos — uploads CPU RGBA frames from backends and optional status-strip HUD.

## Install

```bash
dotnet add package Novolis.Rendering.Presentation.Silk
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), OpenGL 3.3+, Silk.NET windowing/input.

## Quick start

```csharp
using Novolis.Rendering.Presentation.Silk;

SilkGame.Run("Novolis path trace", 1280, 720, ctx =>
{
    if (backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h))
        ctx.FramePresenter.PresentCpuFrame(pixels, ctx.Width, ctx.Height);

    ctx.FramePresenter.ShowStatusStrip = true;
    ctx.SetTitle($"Samples: {backend.SampleCount}");
});
```

Mouse orbit uses `Novolis.Simulation.View.OrbitCameraRig` at the app layer (e.g. SilkTraceStudio). This package provides the window loop, OpenGL presenter, and FPS helper — not camera rigs.

## Quick start — headless pixel sink

```csharp
IFramePresenter sink = new SilkCpuFramePresenter((pixels, w, h) => { /* test hook */ });
```

## API

| Type | Role |
|------|------|
| `SilkGame` | GLFW window loop with initialize/update callbacks |
| `SilkGameContext` | Frame size, delta time, input, `FramePresenter`, title |
| `SilkOpenGlFramePresenter` | OpenGL texture upload + full-screen quad |
| `SilkCpuFramePresenter` | Delegates `PresentCpuFrame` to a callback |
| `SilkOpenGlStatusStrip` | Optional bottom status bar drawing |
| `SilkSmoothedFps` | Exponential moving average FPS |

## Dogfooding / apps

Used by Silk path-tracing samples with `Novolis.Rendering.PathTrace.Demos` for scenes, workers, and display buffers.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Presentation.Abstractions` | `IFramePresenter`, `Key`, `MouseButton` |
| `Novolis.Rendering.PathTrace.Demos` | Shared scenes, session, background worker |
| `Novolis.Rendering.Presentation.Raylib` | Raylib-based presenter instead |
| `Novolis.Rendering.Backends.TwoD.Silk` | Separate 2D platformer stack |

## More documentation

- [Getting started](../../docs/getting-started.md)

