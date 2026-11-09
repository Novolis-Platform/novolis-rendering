# Roadmap — Materials, backends, and interchangeable display

**Status: finished (v1 path-trace experiment closed).**  
Shipped authoring → compile → progressive path trace → present, with CPU + ILGPU backends and host-swappable presentation. Remaining GPU Vulkan *execution* stays outside Rendering (Silk closed island).

Governance: [library-boundaries.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md). Design snapshot: [design.md](design.md), [materials-and-backends.md](materials-and-backends.md).

---

## Exit checklist (done)

| Item | Result |
|------|--------|
| Phase 0 Foundation | Repo, Abstractions, CI builds, boundaries |
| Phase 1 Presentation | `IFramePresenter` + `Presentation.Raylib`; Silk blit via GAME compose |
| Phase 2 Materials | Standard / Glass / Skin / Emissive → `GpuMaterial` |
| Phase 3 Scene + BVH | `SceneCompiler` + Math `TriangleBvh` |
| Phase 4 CPU tracer | Progressive samples; material branches; deterministic goldens |
| Phase 5 DI | `AddRayTracing` / `UseCpu` / `UseIlgpu` / `UseVulkan` (shim) |
| Phase 6a ILGPU | Real kernels + CPU fallback; parity test (deterministic) |
| Phase 6b Vulkan in Rendering | **Intentional CPU shim** — label `Vulkan (CPU fallback)`; Shaderc in `Novolis.Silk.Compute` |
| Phase 6c GL compute | Deferred (optional; not required for v1 exit) |
| Phase 7 Quality | `NOV2005` bans Scene in Raylib; `FramebufferGoldenAssert`; pinned UnitCubeRoom SHA-256; Math BVH |

**Pinned golden (CPU deterministic):** UnitCubeRoom, 32×24, samples 0+1, LookAt `(1.2,0.8,2)→0`, FOV 55° →  
`dbaa0f1b6ef1df3d7fead877150e1428c0d5d27c6589b66ceb07b94979eaa315`

---

## Target architecture (shipped)

```text
┌─────────────────────────────────────────────────────────────────┐
│  novolis-rendering (authoring + compile + backends)             │
│  Scene, Materials, SceneCompiler, CompiledScene                 │
│  IRayTracingBackend — Cpu / Ilgpu / Vulkan(CPU shim)            │
└────────────────────────────┬────────────────────────────────────┘
                             │ IRenderOutput (CPU pixels)
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  Presentation.Abstractions — IFramePresenter                      │
└──────────────┬──────────────────────────────┬───────────────────┘
               ▼                              ▼
   Presentation.Raylib                 GAME + Novolis.Silk
   (texture blit)                      (SilkFrame.Blit / Submit)
```

**Non-negotiable:** scene/materials stay in Rendering. Raylib and Silk never import `Novolis.Rendering.Scene`. Rendering never references Silk.NET (`NOV2016`).

---

## Why Vulkan backend is a CPU shim

Silk.NET may only live in `novolis-silk`. A true Vulkan path tracer would either:

1. Break the island (forbidden), or  
2. Live as GAME composition: Rendering produces `CompiledScene` / CPU frames; `Novolis.Silk.Compute` (`GlslCompiler`) owns SPIR-V; apps upload buffers.

`VulkanRayTracingBackend` preserves the `IRayTracingBackend` + lab “B / 1 / 2 / 3” cycle without lying about GPU execution. Prefer `UseCpuBackend` / `UseIlgpuBackend` for real work.

---

## Labs

| Lab | Host |
|-----|------|
| `RaytraceHello` | Raylib presenter |
| `SilkTraceHello` / `SilkTraceStudio` | Silk blit + backend switch |
| `MeshBench` | Avalonia + path-trace viewport |

```powershell
dotnet run --project d:\novolis\novolis-lab\labs\rendering\SilkTraceStudio\SilkTraceStudio.csproj -p:NovolisUseProjectReferences=true
dotnet test d:\novolis\novolis-rendering\tests\Novolis.Rendering.Unit\Novolis.Rendering.Unit.csproj -p:NovolisUseProjectReferences=true
```

---

## Explicitly out of v1 (future, not blocking)

- Hardware RTX / full Vulkan RT pipeline in Silk.Compute + GAME upload of `CompiledScene`
- Texture atlas / `TextureHandle` authoring
- OpenGL compute backend
- Production GGX / Burley SSS fidelity beyond demo approximations
- Registry republish ceremony beyond normal `merge.yml`

---

## Critical “do not” checklist

| Do not | Why |
|--------|-----|
| Add `Scene` / `IMaterial` to `Novolis.Raylib.*` | Scene is rendering domain |
| Reference Silk.NET from Rendering | Closed Silk island (`NOV2016`) |
| Put `Camera3D` in rendering | Use `CameraSnapshot` |
| Inherit `SkinMaterial : StandardMaterial` | Different transport models |
| Reference Simulation from Rendering | Apps glue `ViewPose` → `CameraSnapshot` |

---

## Related

- [design.md](design.md)
- [materials-and-backends.md](materials-and-backends.md)
- [novolis-rendering](https://github.com/Novolis-Platform/novolis-rendering)
