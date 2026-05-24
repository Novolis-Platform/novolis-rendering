# Roadmap — Materials, backends, and interchangeable display

Plan to evolve [novolis-rendering](https://github.com/Novolis-Platform/novolis-rendering) from the current bootstrap (`CpuRayTracer`, `RenderMesh`) into a full **authoring → compile → trace → present** pipeline, with **Raylib and Silk.NET as swappable display hosts only**.

**Non-negotiable boundary:** scene, materials, `CompiledScene`, and `IRayTracingBackend` live in **rendering**. **Nothing** in `novolis-raylib` references `Scene`, `IMaterial`, or `CompiledScene`. Raylib only implements **presentation** (upload/blit pixels or a host texture handle).

Governance: [library-boundaries.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md).

---

## Target architecture

```text
┌─────────────────────────────────────────────────────────────────┐
│  novolis-rendering (authoring + compile + backends)             │
│  Scene, IMaterial*, Materials.*, SceneCompiler, CompiledScene   │
│  IRayTracingBackend, Cpu/Igpu/Vulkan backends, accumulation     │
└────────────────────────────┬────────────────────────────────────┘
                             │ IRenderOutput (pixels or opaque GPU id)
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  Novolis.Rendering.Presentation.Abstractions                    │
│  IFramePresenter — host-neutral “show this frame” contract        │
└──────────────┬──────────────────────────────┬───────────────────┘
               │                              │
               ▼                              ▼
┌──────────────────────────┐    ┌──────────────────────────┐
│ Novolis.Raylib.          │    │ Novolis.Silk.            │
│ Presentation             │    │ Presentation             │
│ (novolis-raylib repo)    │    │ (novolis-silk or         │
│                          │    │  rendering optional pkg) │
│ Texture upload + blit    │    │ GL/Vulkan blit           │
│ NO Scene / Material      │    │ NO Scene / Material      │
└──────────────────────────┘    └──────────────────────────┘
               │                              │
               ▼                              ▼
         Raylib window                   Silk window / swapchain
```

**App / dogfood** wires: `ViewPose` → `CameraSnapshot` → backend → presenter → host loop.

---

## Package map (end state)

| Package | Repo | Depends on | Must NOT reference |
|---------|------|------------|-------------------|
| `Novolis.Rendering.Abstractions` | rendering | Math | Raylib, Simulation, Silk |
| `Novolis.Rendering.Materials` | rendering | Abstractions | GPU APIs |
| `Novolis.Rendering.Scene` | rendering | Materials, Math | GPU APIs |
| `Novolis.Rendering.Compile` | rendering | Scene, Materials | GPU APIs |
| `Novolis.Rendering.Runtime` | rendering | Compile | GPU APIs (structs only) |
| `Novolis.Rendering.Backends.Cpu` | rendering | Runtime | Raylib, Silk |
| `Novolis.Rendering.Backends.Igpu` | rendering | Runtime, ILGPU | Raylib, Silk |
| `Novolis.Rendering.Backends.Vulkan` | rendering | Runtime, Silk.NET.Vulkan | Raylib |
| `Novolis.Rendering` | rendering | meta | — |
| `Novolis.Rendering.Presentation.Abstractions` | rendering | Abstractions | Raylib, Silk |
| `Novolis.Rendering.DependencyInjection` | rendering | Backends.*, Presentation.Abstractions | Raylib, Silk |
| `Novolis.Raylib.Presentation` | **raylib** | Rendering.Presentation.Abstractions, Raylib.Runtime | **Rendering.Scene**, Materials |
| `Novolis.Silk.Presentation` | silk or rendering | Presentation.Abstractions, Silk | Scene, Materials |

**Migrate / rename today:**

| Current | Target |
|---------|--------|
| `Novolis.Rendering.Raytrace` / `CpuRayTracer` | `Novolis.Rendering.Backends.Cpu` |
| `IRayTracer` + `RenderScene` | superseded by `IRayTracingBackend` + `CompiledScene` |
| `RenderMesh` / per-mesh color | `Scene` authoring + `GpuTriangle` + material indices |

Keep `IRayTracer` as obsolete shim until Phase 2 completes.

---

## Two worlds (authoring vs runtime)

```text
Authoring (human-friendly)          Runtime (flat, blittable)
─────────────────────────          ─────────────────────────
Scene                              CompiledScene
IMaterial records                  GpuMaterial[]
MeshInstance + material ref        GpuTriangle[]
Light definitions                  GpuLight[]
                                   BvhNode[]
        SceneCompiler.Compile()
```

**Rules**

1. Backends consume **only** `CompiledScene` + `CameraSnapshot`.
2. Authoring types never appear in hot paths or GPU kernels.
3. `GpuMaterial` is fixed-size (`Vector4` × 3 + `MaterialModel`); no interfaces in runtime arrays.
4. Materials describe **light transport**, not gameplay tags, physics, or chemistry.

---

## Phase 0 — Foundation (done / stabilize)

**Goal:** Repo builds, tests, CI, local pack; boundary documented.

- [x] `novolis-rendering` repo, `Novolis.Rendering.Abstractions`, bootstrap `CpuRayTracer`
- [x] Governance entry for rendering ⊥ raylib
- [ ] CI: ensure `Novolis.Math.Geometry` available on CI feed (pack math in workflow or publish pin)
- [ ] Add `docs/materials-and-backends.md` (this spec, trimmed) as normative API reference

**Exit:** `dotnet build` + TUnit green on main.

---

## Phase 1 — Presentation layer (Raylib + Silk interchangeable)

**Goal:** Same traced pixels on screen via either host; **zero** scene leakage into raylib.

### 1a — `Novolis.Rendering.Presentation.Abstractions`

```csharp
/// <summary>Displays a finished frame. Implementations live in host repos.</summary>
public interface IFramePresenter
{
    void PresentCpuFrame(ReadOnlySpan<Rgba32> pixels, int width, int height);
}

/// <summary>Optional: backends that render directly to a GPU resource.</summary>
public interface IGpuFramePresenter
{
    void PresentGpuFrame(IRenderGpuSurface surface);
}

public interface IRenderGpuSurface
{
    IntPtr NativeHandle { get; } // interpreted only by the matching presenter
    int Width { get; }
    int Height { get; }
}
```

- `IRenderOutput` on backend: `TryGetCpuPixels()` OR `TryGetGpuSurface()` — CPU path always available for tests.

### 1b — `Novolis.Raylib.Presentation` (in **novolis-raylib**)

- New optional package; references `Novolis.Rendering.Presentation.Abstractions` + `Novolis.Raylib.Runtime`.
- `RaylibCpuFramePresenter` — `LoadImageEx` / `UpdateTexture` / `DrawTexture` from `Rgba32[]`.
- Extension: `RayGameContext.PresentFrame(IFramePresenter, ImageBuffer)`.
- **Forbidden in this package:** `using Novolis.Rendering.Scene`, `Materials`, `Compile`, any `CompiledScene`.

### 1c — `Novolis.Silk.Presentation`

- Same contract as 1b; Silk.NET OpenGL or Vulkan swapchain blit.
- Repo choice: `novolis-silk` (preferred long-term) or `Novolis.Rendering.Presentation.Silk` until silk host exists.

### 1d — Dogfood sample `RaytraceHello`

- App references: Rendering (cpu backend) + Raylib.Game + Raylib.Presentation.
- Loop: compile stub scene → render → `PresentCpuFrame` → HUD via existing Raylib.

**Exit:** Toggle presenter implementation in DI without changing scene code; grep confirms no `Scene` in `novolis-raylib/src`.

---

## Phase 2 — Materials (authoring) + compilation

**Goal:** Spec material models + `Materials.*` presets; compile to `GpuMaterial`.

### 2a — `Novolis.Rendering.Materials`

```csharp
public interface IMaterial { }

public sealed record StandardMaterial : IMaterial { /* spec fields */ }
public sealed record GlassMaterial : IMaterial { /* spec */ }
public sealed record SkinMaterial : IMaterial { /* spec */ }
public sealed record EmissiveMaterial : IMaterial { /* spec */ }

public static class Materials
{
    public static StandardMaterial Standard(...) => ...;
    public static StandardMaterial Metal(...) => ...;
    public static GlassMaterial Glass(...) => ...;
    // ...
}
```

- No inheritance between material types (only `IMaterial`).
- Unit tests: preset → expected `StandardMaterial` field values.

### 2b — `MaterialModel` + `GpuMaterial` + `MaterialCompiler`

```csharp
public enum MaterialModel { Standard, Glass, Skin, Emissive }

public readonly record struct GpuMaterial
{
    public MaterialModel Model;
    public Vector4 A, B, C;
}
```

- `MaterialCompiler.Compile(IMaterial)` → `GpuMaterial` with documented packing per model.
- Golden tests: known material → fixed `A/B/C` floats.

### 2c — Textures (deferred)

- `MaterialTextures` + `TextureHandle` as **opaque int** in rendering only.
- No file I/O in rendering; apps load via Raylib/Silk and pass handles at compile time (Phase 6).

**Exit:** All four material types compile; no GPU types in Materials package.

---

## Phase 3 — Scene authoring + `CompiledScene`

**Goal:** Replace `RenderMesh` / `RenderScene` with spec-aligned authoring and runtime.

### 3a — `Novolis.Rendering.Scene`

```csharp
public sealed class Scene
{
    public IList<MeshInstance> Meshes { get; }
    public IList<LightDefinition> Lights { get; }
}

public sealed record MeshInstance(
    ReadOnlyMemory<Vector3> Vertices,
    ReadOnlyMemory<int> Indices,
    IMaterial Material,
    Matrix4x4 Transform);
```

- Builders: `SceneBuilder.AddBox`, `AddGroundPlane`, etc.
- Lights: directional, point (minimal set for Phase 3).

### 3b — `Novolis.Rendering.Compile`

```csharp
public static class SceneCompiler
{
    public static CompiledScene Compile(Scene scene);
}

public sealed record CompiledScene
{
    public required ImmutableArray<GpuTriangle> Triangles { get; init; }
    public required ImmutableArray<GpuMaterial> Materials { get; init; }
    public required ImmutableArray<GpuLight> Lights { get; init; }
    public required ImmutableArray<BvhNode> BvhNodes { get; init; }
}
```

- `GpuTriangle(Vector4 A, B, C, int MaterialIndex)` — world-space after transform.
- BVH: port SAH builder from `Novolis.Physics.Collision.Simple` logic into **Math** or **Rendering.Compile** (prefer extract to `Novolis.Math.Geometry` per governance).

### 3c — Deprecate bootstrap types

- Obsolete `RenderMesh`, `RenderScene`, `IRayTracer`; adapter maps old API → compile → backend for one release.

**Exit:** `SceneCompiler` tests: triangle count, material indices, BVH hit consistency vs brute force.

---

## Phase 4 — `IRayTracingBackend` + CPU path tracer v2

**Goal:** Backend interface from spec; progressive accumulation; physically clearer shading.

### 4a — Abstractions

```csharp
public readonly record struct CameraSnapshot(
    Vector3 Position, Vector3 Forward, Vector3 Right, Vector3 Up,
    float VerticalFovRadians, float AspectRatio);

public interface IRayTracingBackend
{
    ValueTask ResizeAsync(int width, int height, CancellationToken ct = default);
    ValueTask UploadSceneAsync(CompiledScene scene, CancellationToken ct = default);
    ValueTask RenderAsync(CameraSnapshot camera, int sampleIndex, CancellationToken ct = default);
    IRenderOutput Output { get; }
    int SampleCount { get; }
    void ResetAccumulation();
}
```

### 4b — `CpuRayTracingBackend`

- `Parallel.For` over pixels; `ArrayPool` for ray stacks.
- Per material model switch in shading (start with Standard + Emissive; Glass/Skin simplified then refined).
- Progressive: `accumulation += sample; display = accumulation / sampleCount`.
- Deterministic mode for golden PNG tests (fixed seed, single thread).

### 4c — Shading milestones

| Milestone | Models | Technique |
|-----------|--------|-----------|
| M1 | Standard | Lambert + ambient + one directional |
| M2 | Standard | GGX microfacet (metallic/roughness) |
| M3 | Emissive | Mesh lights |
| M4 | Glass | Refraction (single bounce) |
| M5 | Skin | Approximate SSS (burley-style or diffusion) |

**Exit:** Dogfood demo with progressive refine; golden tests per milestone.

---

## Phase 5 — Dependency injection + backend selection

**Goal:** `services.AddRayTracing().UseCpuBackend()` without pulling Raylib.

```csharp
// Novolis.Rendering.DependencyInjection
public static IServiceCollection AddRayTracing(this IServiceCollection services)
{
    services.AddSingleton<SceneCompiler>();
    return services;
}

public static IServiceCollection UseCpuBackend(this IServiceCollection services)
{
    services.AddSingleton<IRayTracingBackend, CpuRayTracingBackend>();
    return services;
}
```

- Separate extension packages or conditions: `UseIlgpuBackend`, `UseVulkanBackend` (optional NuGet deps).
- Presentation registered in **app** or host repo:

```csharp
services.AddRayTracing().UseCpuBackend();
services.AddSingleton<IFramePresenter, RaylibCpuFramePresenter>();
```

**Exit:** RandoriFight-scale app could swap `IFramePresenter` registration only.

---

## Phase 6 — GPU backends (ILGPU, then Silk Vulkan)

**Goal:** Same `CompiledScene` uploaded to GPU buffers; same `IRayTracingBackend` contract.

### 6a — `Novolis.Rendering.Backends.Igpu`

- Flat buffers mirror `GpuTriangle`, `GpuMaterial`, BVH nodes.
- C# kernels for trace + shade (parity tests vs CPU on small scenes).

### 6b — `Novolis.Rendering.Backends.Vulkan` (Silk.NET)

- Compute pipeline; no scene types in Silk package.
- `IRenderGpuSurface` implemented by backend; `SilkPresentation` blits handle.

### 6c — OpenGL compute (optional)

- Same as Vulkan but GL 4.3 path for broader hardware.

**Exit:** Backend parity test suite: same `CompiledScene` + camera → images within tolerance (PSNR/SSIM threshold).

---

## Phase 7 — Quality, tooling, governance

- **Analyzers:** ban `Novolis.Rendering.Scene` imports in `novolis-raylib` (Roslyn analyzer in `novolis-analyzers` or repo-local).
- **Golden harness:** PNG SHA256 over `IRenderOutput` CPU path (no native window).
- **Registry:** publish packages to `novolis-registry`.
- **Governance:** extend `library-boundaries.md` with material/scene placement table.
- **BVH in Math:** extract shared BVH from Physics → `Novolis.Math.Geometry`; Physics and Rendering.Compile both use it.

---

## Critical “do not” checklist

| Do not | Why |
|--------|-----|
| Add `Scene` / `IMaterial` to `Novolis.Raylib.*` | Scene is rendering domain; raylib is display/input |
| Let `IRaylibFrameRenderer` accept `CompiledScene` | Keeps frame hook host-agnostic |
| Put `Camera3D` in rendering | GPU type; use `CameraSnapshot` |
| Inherit `SkinMaterial : StandardMaterial` | Different transport models |
| Expose descriptor sets / pipelines in authoring APIs | Breaks backend swap |
| Reference Simulation from Rendering | Apps glue `ViewPose` → `CameraSnapshot` |

---

## Suggested implementation order (sprints)

```text
Sprint 1   Phase 1  Presentation abstractions + Raylib.Presentation + RaytraceHello
Sprint 2   Phase 2  Materials + MaterialCompiler + tests
Sprint 3   Phase 3  Scene + SceneCompiler + BVH
Sprint 4   Phase 4  IRayTracingBackend + CPU M1–M2 + progressive
Sprint 5   Phase 5  DI + dogfood integration (Artillery or new sample)
Sprint 6   Phase 4  CPU M3–M5 (glass, skin, emissive lights)
Sprint 7   Phase 1c Silk.Presentation (swap demo)
Sprint 8   Phase 6  ILGPU backend
Sprint 9   Phase 6  Vulkan/Silk compute
Sprint 10  Phase 7  Analyzers, registry, Math BVH extraction
```

---

## App integration pattern (stable)

```csharp
// 1. Author (rendering only)
var scene = new SceneBuilder()
    .AddGround(Materials.Standard(...))
    .AddSphere(Materials.Metal(Colors.Silver, 0.08f))
    .Build();

var compiled = sceneCompiler.Compile(scene);

// 2. Trace (rendering only)
await backend.UploadSceneAsync(compiled);
await backend.RenderAsync(cameraSnapshot, sampleIndex);

// 3. Present (host package only — Raylib OR Silk)
presenter.PresentCpuFrame(backend.Output.GetCpuPixels());

// 4. Simulation cameras (optional, app layer)
var camera = CameraSnapshot.FromObserver(viewPose, aspect);
```

---

## Open decisions (resolve before Phase 1b)

1. **Silk repo name:** `novolis-silk` vs presentation-only package under rendering.
2. **`TextureHandle`:** `int` id + registry in rendering vs defer until Phase 6c.
3. **BVH ownership:** move to Math in Phase 3 vs duplicate in Compile temporarily.
4. **Package version alignment:** 0.0.1.1 org versioning vs `-local` monorepo feeds (already used elsewhere).

---

## Related

- [design.md](design.md) — current bootstrap scope
- [getting-started.md](getting-started.md) — build instructions
- [novolis-rendering](https://github.com/Novolis-Platform/novolis-rendering)
- [novolis-raylib](https://github.com/Novolis-Platform/novolis-raylib)
- [Silk.NET](https://github.com/dotnet/Silk.NET)
