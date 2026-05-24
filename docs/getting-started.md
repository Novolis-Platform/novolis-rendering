# Getting started

## Prerequisites

- .NET SDK **10.0.100+** (`global.json`)
- GitHub Packages credentials for `Novolis.*` (see [nuget-setup.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/nuget-setup.md))
- Repo `nuget.config`: **nuget.org** + **github** only — no local feeds

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

## Publishing

Library releases go to **GitHub Packages** via merge to `main` and the repo publish workflow — not to a local folder feed.
