# Novolis.Rendering.PathTrace.Profile

Stage profiler for interactive ILGPU path tracing (Toub-style: wall time + allocations per stage).

Not a BenchmarkDotNet suite — GPU present cost is dominated by stream sync + CPU readback, which this host isolates explicitly.

## Run

```powershell
dotnet run -c Release --project d:\novolis\novolis-rendering\benchmarks\Novolis.Rendering.PathTrace.Profile\Novolis.Rendering.PathTrace.Profile.csproj -p:NovolisUseProjectReferences=true

dotnet run -c Release --project d:\novolis\novolis-rendering\benchmarks\Novolis.Rendering.PathTrace.Profile\Novolis.Rendering.PathTrace.Profile.csproj -p:NovolisUseProjectReferences=true -- --width 480 --height 270 --iters 64 --json
```

## How to read the table

| Stage | Meaning |
| --- | --- |
| `gpu-only` | Kernel enqueue only (does **not** wait for the GPU) |
| `flush-only` | Deferred kernel execution + DMA readback (the real present tax) |
| `moving-1spp-loop` | Full interactive preview: 1 spp + flush + publish |
| `orbit-bucket-4spp` | Auto-orbit accumulate bucket |
| `publish` / `copy-if-new` | CPU display-buffer copies after readback |

## Academic takeaway (RTX 4070)

Interactive path tracing on raster hosts is almost always **present-bound**: one GPU→CPU readback (then GL upload) dominates. Kernel enqueue is ~0.02 ms; `exec+readback` is milliseconds.

Wrong patterns that keep the card idle:
1. **Cancel/restart every UI frame** while orbiting → samples stuck at 1, SM thrash.
2. **One sample + one readback** per pose → pays the DMA tax for almost no radiance work.
3. **Accumulate-with-frozen-camera** for smoothness → stepped motion.

Right pattern: **coalesce camera**, finish a **multi-spp GPU batch**, **one readback**, then take the latest pending pose. Longer term: CUDA–GL interop removes the bounce.

On an RTX 4070 @ interactive res, expect `exec+readback` ≈ 99% of present cost. Raise samples **inside** each batch before flushing — that is how you spend the 4070.

## Quality diagnosis (muddy stills @ high spp)

If the image stays grainy at hundreds of samples, the bottleneck is **estimator variance**, not GPU throughput:

1. **Emissive geometry without NEE** — `DirectLighting` only samples `GpuLight`. Ceiling panels / neon sticks that are *only* emissive meshes light the room via lucky bounces → salt-and-pepper forever. Fix: `SceneCompiler` extracts emissive meshes to point lights (and point falloff `1/r²`).
2. **Shadow rays occluding the light itself** — proxies sit *inside* emissive boxes, so the first hit was the light mesh → every NEE sample discarded. Fix: treat emissive hits as clear in shadow tests.
3. **Bright open-face environment** — HDR sky through the diorama mouth is an unsampled light. Fix: dim `SampleSky` (`EnvStrength`).
4. **Directional sun + interior emissives** — competing estimators. Studio scene is emissive-lit only.
5. Present cost being low while mud remains confirms you are **sample-efficiency** bound, not SM-bound.
