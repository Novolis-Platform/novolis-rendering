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

On an RTX 4070 @ 320×180, expect `exec+readback` ≈ 99% of present cost and `moving-1spp-loop` ≈ 0.6 ms (~1.5 kHz ceiling before window/UI overhead). The remaining win is eliminating GPU→CPU→GL entirely (CUDA–GL interop) — not more kernel micro-opts.
