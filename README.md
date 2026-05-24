# novolis-rendering

**Graphics-host-neutral frame production** — CPU ray tracing and framebuffer contracts. Computes RGBA frames; **does not** own windows, GPU draw calls, or input.

Display adapters (Raylib texture upload, Silk.NET, …) live in host repos or apps. See [library boundaries](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md) — Rendering is orthogonal to `novolis-raylib` and `novolis-simulation`, same as Simulation ↔ Raylib.

## Packages

| Package | Role |
|---------|------|
| `Novolis.Rendering.Abstractions` | `ImageBuffer`, `RenderCamera`, `RenderScene`, `IRayTracer` |
| `Novolis.Rendering.Raytrace` | `CpuRayTracer` — primary-ray CPU renderer |
| `Novolis.Rendering` | Meta package referencing abstractions + raytrace |

## Build

```powershell
cd d:\novolis\novolis-math
.\scripts\pack-local.ps1
cd d:\novolis\novolis-rendering
dotnet build Novolis.Rendering.slnx
dotnet run --project tests/Novolis.Rendering.Raytrace.Tests
.\scripts\pack-local.ps1
```

## Compose with Raylib (app layer)

```csharp
// 1. Simulation (optional): ViewPose from Novolis.Simulation.View
var camera = RenderCamera.FromObserver(pose.Position, pose.Target, pose.Up, pose.FieldOfViewDegrees, aspect);

// 2. Render CPU frame
_tracer.Render(_buffer, camera, scene);

// 3. Present via Raylib (adapter in app or future Novolis.Raylib.Presentation)
// Upload _buffer.Pixels to a texture and DrawTexture — not part of this repo.
```

## Policy

- No reference to `Novolis.Raylib.*` or `Novolis.Simulation.*` from platform Rendering packages.
- Apps wire observers → `RenderCamera` → `IRayTracer` → host presenter.

Repository: [github.com/Novolis-Platform/novolis-rendering](https://github.com/Novolis-Platform/novolis-rendering)
