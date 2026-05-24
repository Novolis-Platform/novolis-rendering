# Materials and ray tracing backends

Normative API reference for `novolis-rendering`. Host display (Raylib, Silk.NET) is **out of scope** here — see [roadmap-raytracing.md](roadmap-raytracing.md).

## Two worlds

| Authoring | Runtime |
|-----------|---------|
| `Scene`, `IMaterial`, `MeshInstance` | `CompiledScene` |
| Human-friendly records | `GpuTriangle`, `GpuMaterial`, `GpuLight`, `BvhNode` |
| `Materials.Metal(...)` presets | Fixed-size blittable structs |

Backends consume **only** `CompiledScene` + `CameraSnapshot`.

## Materials

`IMaterial` is a marker. Concrete models are independent records (no inheritance):

- `StandardMaterial` — PBR baseline (base color, roughness, metallic, emission)
- `GlassMaterial` — transmission / IOR
- `SkinMaterial` — approximate subsurface
- `EmissiveMaterial` — mesh lights

`MaterialCompiler.Compile(IMaterial)` → `GpuMaterial` with `MaterialModel` + three `Vector4` lanes.

## Backend contract

```csharp
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

Progressive rendering: accumulate samples; display = average.

## Presentation (separate packages)

`IFramePresenter.PresentCpuFrame(ReadOnlySpan<Rgba32>, width, height)` — implemented in `Novolis.Rendering.Presentation.Raylib` or `Novolis.Rendering.Presentation.Silk`. **No scene types in host presenter packages.**

## Boundaries

- Rendering must not reference `Novolis.Raylib.*` or `Novolis.Simulation.*`.
- `novolis-raylib` must not reference any `Novolis.Rendering.*` package.
- Only `Novolis.Rendering.Presentation.Raylib` may reference `Novolis.Raylib.Runtime`.
