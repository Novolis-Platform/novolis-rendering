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
dotnet run --project tests/Novolis.Rendering.Unit
```

## Minimal render

```csharp
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Runtime;

var backend = new CpuRayTracingBackend(deterministic: true);
await backend.ResizeAsync(320, 240);
await backend.UploadSceneAsync(DemoSceneFactory.UnitCubeRoom());

var camera = CameraSnapshot.LookAt(
    eye: new(2f, 1f, 3f),
    target: Vector3.Zero,
    up: Vector3.UnitY,
    verticalFovDegrees: 60f,
    aspectRatio: 320f / 240f);

await backend.RenderAsync(camera, 0);
// backend.Output → hand off to your presenter (Raylib, Silk.NET, file export, …)
```

## Pack locally

```powershell
.\scripts\pack-local.ps1
```
