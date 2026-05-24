# Getting started

## Prerequisites

- .NET SDK **10.0.100+** (`global.json`)
- Local `Novolis.Math.Geometry` on the feed (sibling checkout):

```powershell
cd d:\novolis\novolis-math
.\scripts\pack-local.ps1
```

Set `NOVOLIS_LOCAL_FEED` or use the default `..\artifacts\nuget-local` next to this repo.

## Build and test

```powershell
cd d:\novolis\novolis-rendering
dotnet build Novolis.Rendering.slnx
dotnet run --project tests/Novolis.Rendering.Abstractions.Tests
dotnet run --project tests/Novolis.Rendering.Raytrace.Tests
```

## Minimal render

```csharp
using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Raytrace;

var buffer = new ImageBuffer(320, 240);
var camera = RenderCamera.LookAt(
    position: new(2f, 1f, 3f),
    target: Vector3.Zero,
    up: Vector3.UnitY,
    verticalFovDegrees: 60f,
    aspectRatio: 320f / 240f);

IRayTracer tracer = new CpuRayTracer();
tracer.Render(buffer, camera, RenderSceneFactory.UnitCubeRoom());

// buffer.Pixels → hand off to your presenter (Raylib, Silk.NET, file export, …)
```

## Pack locally

```powershell
.\scripts\pack-local.ps1
```
