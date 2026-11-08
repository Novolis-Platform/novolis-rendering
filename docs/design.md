# Design

## Role

`novolis-rendering` owns **authoring → compile → trace** for CPU (and ILGPU) path tracing, plus planar tessellation and appearance helpers. It does **not** open windows or reference Silk.NET / Raylib outside the blessed `Presentation.Raylib` blit adapter.

## Dependency rules

```text
novolis-math (Geometry, BVH)  →  novolis-rendering
novolis-rendering             ⊥  novolis-raylib   (except Presentation.Raylib → Raylib.Runtime)
novolis-rendering             ⊥  novolis-silk     (NOV2016; handshake = Math/BCL pixels)
novolis-rendering             ⊥  novolis-simulation
```

Apps glue:

```text
Simulation.View (ViewPose) → CameraSnapshot → IRayTracingBackend → IFramePresenter / SilkFrame.Blit
```

## Path-trace packages (shipped)

| Package | Owns |
|---------|------|
| `Novolis.Rendering.Materials` | `IMaterial` presets → `GpuMaterial` |
| `Novolis.Rendering.Scene` | Authoring `Scene` / builders |
| `Novolis.Rendering.Compile` | `SceneCompiler` → `CompiledScene` + BVH |
| `Novolis.Rendering.Runtime` | `IRayTracingBackend`, `CompiledScene`, GPU structs |
| `Novolis.Rendering.Backends.Cpu` | Progressive CPU path tracer (golden reference) |
| `Novolis.Rendering.Backends.Igpu` | ILGPU kernels + CPU fallback |
| `Novolis.Rendering.Backends.Vulkan` | **CPU contract shim** — real Vulkan/Shaderc in `Novolis.Silk.Compute` |
| `Novolis.Rendering.Presentation.Abstractions` | `IFramePresenter`, `IRenderOutput` |
| `Novolis.Rendering.Presentation.Raylib` | CPU frame blit to Raylib |
| `Novolis.Rendering.DependencyInjection` | `AddRayTracing` / `Use*Backend` |
| `Novolis.Rendering.PathTrace.Demos` | Studio scenes + background worker |
| `Novolis.Rendering.Testing` | SHA-256 golden hashes (no window) |

Planar stack: **[design-planar.md](design-planar.md)**. Roadmap closure: **[roadmap-raytracing.md](roadmap-raytracing.md)**.

## Presentation

- Raylib: `RaylibCpuFramePresenter`
- Silk: compose in GAME — `SilkFrame.Blit` / `Submit` (no Rendering→Silk package)

## Acceleration

BVH lives in `Novolis.Math.Geometry` (`TriangleBvhBuilder`); Compile emits runtime nodes for backends.

## Related

- [library-boundaries.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md)
- Labs: `SilkTraceStudio`, `SilkTraceHello`, `RaytraceHello`, `MeshBench`
