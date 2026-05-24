# Novolis.Rendering.Presentation.Silk

Silk.NET window loop and OpenGL presenters for path-tracing demos.

## Install

```bash
dotnet add package Novolis.Rendering.Presentation.Silk
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), OpenGL 3.3+.

## Quick start

```csharp
using Novolis.Rendering.Presentation.Silk;

SilkGame.Run("Novolis path trace", 1280, 720, ctx =>
{
    ctx.FramePresenter.PresentCpuFrame(pixels, ctx.Width, ctx.Height);
    ctx.FramePresenter.ShowStatusStrip = true; // optional HUD bar
    ctx.SetTitle($"Samples: {sampleCount}");
});
```

Mouse orbit, smoothed FPS, and backend hotkeys are used by the **SilkTraceStudio** dogfood app (`Novolis.Rendering.PathTrace.Demos` + `SilkOrbitCamera`).

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Presentation.Abstractions` | `IFramePresenter` contract |
| `Novolis.Rendering.PathTrace.Demos` | Shared scenes, workers, session for Silk/Raylib samples |
| `Novolis.Rendering.Presentation.Raylib` | Raylib-based presenter instead |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
