# Novolis.Rendering.PathTrace.Demos

Shared path-tracing demo scenes, async background workers, display buffers, and session helpers for Silk and Raylib dogfood apps.

## Install

```bash
dotnet add package Novolis.Rendering.PathTrace.Demos
```

## Quick start

```csharp
using Novolis.Rendering.PathTrace.Demos;

var scene = ShowcaseScenes.BuildHelloShowcase();
using var session = new PathTraceSession(scene);
var display = new PathTraceDisplayBuffer();
using var worker = new PathTraceBackgroundWorker(session.Backend, display);

session.Resize(1280, 720);
display.Invalidate(1280, 720);
// enqueue samples via worker, then display.TryPresent(presenter);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Rendering.Presentation.Silk` | Silk window + OpenGL presenter |
| `Novolis.Rendering.DependencyInjection` | `AddRayTracingFromEnvironment()` |
