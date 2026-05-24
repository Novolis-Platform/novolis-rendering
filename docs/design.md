# Design

## Role

`novolis-rendering` produces **CPU framebuffers** (`Rgba32` pixels). It is not a game engine and does not replace `novolis-raylib` (GPU host) or `novolis-simulation` (worlds, clocks, cameras).

## Dependency rules

```text
novolis-math (Geometry)  →  novolis-rendering
novolis-rendering        ⊥  novolis-raylib
novolis-rendering        ⊥  novolis-simulation
```

Apps may reference any combination and own glue:

```text
Simulation.View (ViewPose) → RenderCamera → IRayTracer → ImageBuffer → Raylib/Silk presenter
```

## Packages

| Package | Depends on | Owns |
|---------|------------|------|
| `Novolis.Rendering.Abstractions` | `Novolis.Math.Geometry` | Buffers, camera, scene DTOs, `IRayTracer` |
| `Novolis.Rendering.Raytrace` | Abstractions, Math | `CpuRayTracer`, intersection helpers |
| `Novolis.Rendering` | Facets above | Meta / convenience reference |
| `Novolis.Rendering.TwoD` | Math.Geometry, Math.Topology | 2D scene, sprites, collision, HUD, menus |
| `Novolis.Rendering.Backends.TwoD.Silk` | TwoD, Silk.NET | OpenGL orthographic renderer + game loop |

See **[design-two-d.md](design-two-d.md)** for the Mario-style 2D stack.

## Roadmap

Full phased plan (materials, compile pipeline, backends, Raylib/Silk presenters):

**[roadmap-raytracing.md](roadmap-raytracing.md)**

## Presentation (out of scope in core packages)

Optional host packages — **no scene/material types**:

- `Novolis.Rendering.Presentation.Raylib` — `IFramePresenter` → Raylib texture blit
- `Novolis.Silk.Presentation` — same contract via Silk.NET

Golden tests for the tracer use PNG hashes over CPU pixels without native Raylib.

## Acceleration

`CpuRayTracer` v1 uses brute-force triangle tests. BVH traversal may move to Math (shared with Physics collision) without changing `IRayTracer`.

## Related

- [library-boundaries.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md)
- [novolis-raylib](https://github.com/Novolis-Platform/novolis-raylib) — display path
