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

## Presentation (out of scope here)

Future optional packages:

- `Novolis.Raylib.Presentation` — upload `ImageBuffer` to `Texture`, blit in `IRenderSystem`
- `Novolis.Silk.Presentation` — same contract, different API

Golden tests for the tracer use PNG hashes over `ImageBuffer` bytes without native Raylib.

## Acceleration

`CpuRayTracer` v1 uses brute-force triangle tests. BVH traversal may move to Math (shared with Physics collision) without changing `IRayTracer`.

## Related

- [library-boundaries.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md)
- [novolis-raylib](https://github.com/Novolis-Platform/novolis-raylib) — display path
