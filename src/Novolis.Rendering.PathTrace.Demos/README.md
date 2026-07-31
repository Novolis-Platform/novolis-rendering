# Novolis.Rendering.PathTrace.Demos

Shared path-tracing demo infrastructure: prebuilt showcase scenes, backend selection, session lifecycle, background sample workers, and a thread-safe display buffer for Silk and Raylib dogfood apps.

## Install

```bash
dotnet add package Novolis.Rendering.PathTrace.Demos
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`). Pulls CPU, ILGPU, and Vulkan backend packages for `PathTraceBackendFactory`.

## Quick start — interactive session

```csharp
using Novolis.Rendering.PathTrace.Demos;
using Novolis.Rendering.Runtime;

var scene = ShowcaseScenes.BuildHelloShowcase();
using var session = new PathTraceSession(scene);
var display = new PathTraceDisplayBuffer();
using var worker = new PathTraceBackgroundWorker(session.Backend, display);

session.Resize(1280, 720);
display.Invalidate(1280, 720);

var camera = CameraSnapshot.LookAt(
    ShowcaseScenes.HelloDefaultEye,
    ShowcaseScenes.OrbitTarget,
    Vector3.UnitY,
    verticalFovDegrees: 60f,
    aspectRatio: 1280f / 720f);

worker.EnqueueOrbit(camera, samplesPerFrame: 4);
// In the host loop: display.TryPresent(presenter);
```

## Quick start — backend selection

```csharp
// Environment: NOVOLIS_RAY_BACKEND=cpu|ilgpu|vulkan (default ilgpu)
using var session = new PathTraceSession(scene, PathTraceBackendKind.Cpu);
session.SwitchBackend(PathTraceBackendKind.Vulkan);
var kind = session.CycleBackend(); // cpu → ilgpu → vulkan → cpu
```

Progressive accumulation: `worker.TryEnqueueAccumulate(camera, ref sample, batchSize)` queues batches when idle.

## Quick start — window title HUD

```csharp
var title = PathTraceStatusTitle.Format(
    session.Backend,
    display.DisplayedSampleCount,
    orbitEnabled: true,
    fps: 60f,
    rendering: worker.IsBusy);
```

## API

| Type | Role |
|------|------|
| `ShowcaseScenes` | `BuildHelloShowcase()`, `BuildStudioShowcase()`, orbit/eye constants |
| `PathTraceSession` | Owns backend, resize, upload, backend switch/cycle |
| `PathTraceBackendFactory` | `Create`, `FromEnvironment`, `Parse` |
| `PathTraceBackendKind` | `Ilgpu`, `Vulkan`, `Cpu` |
| `PathTraceBackgroundWorker` | Background orbit/accumulate jobs, `ReplaceBackend` |
| `PathTraceDisplayBuffer` | Thread-safe CPU frame buffer + `TryPresent` |
| `PathTraceStatusTitle` | Formats demo title-bar status strings |

## Dogfooding / apps

Used by Silk and Raylib path-tracing samples (`SilkTraceStudio`, `RaytraceHello`). Orbit camera rigs live in app layers (`Novolis.Simulation.View`), not this package.

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Presentation.Silk` | Silk window + OpenGL presenter |
| `Novolis.Rendering.Presentation.Raylib` | Raylib CPU frame presenter |
| `Novolis.Rendering.DependencyInjection` | `AddRayTracingFromEnvironment()` |
| `Novolis.Rendering.Runtime` | `IRayTracingBackend`, `CameraSnapshot`, `CompiledScene` |
