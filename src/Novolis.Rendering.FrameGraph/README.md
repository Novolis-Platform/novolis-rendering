<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-rendering/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-rendering/) · [Source](https://github.com/Novolis-Platform/novolis-rendering)
<!-- novolis-pkg-brand:end -->

# Novolis.Rendering.FrameGraph

Renderer-owned per-frame contracts for typed textures and buffers, attachment declarations, pass dependencies, resource lifetimes, barriers, quality profiles, resize, and deterministic diagnostics.

The package is host-neutral. It has no UI toolkit, native graphics API, simulation, workflow, or scene compiler dependency. It describes and validates frame work; it does not replace `SceneCompiler`, `CompiledScene`, or `IRayTracingBackend`.

## Install

```bash
dotnet add package Novolis.Rendering.FrameGraph
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Rendering.FrameGraph;

var graph = new FrameGraphBuilder("Lighting", new FrameGraphSize(1280, 720));
var hdr = graph.CreateTexture(
    "HdrColor",
    FrameGraphTextureDescription.Output(
        FrameGraphFormat.Rgba16Float,
        FrameGraphResourceUsage.ColorAttachment | FrameGraphResourceUsage.Sampled));
var depth = graph.CreateTexture(
    "Depth",
    FrameGraphTextureDescription.Output(
        FrameGraphFormat.D32Float,
        FrameGraphResourceUsage.DepthStencilAttachment | FrameGraphResourceUsage.Sampled));
var output = graph.CreateTexture(
    "Output",
    FrameGraphTextureDescription.Output(
        FrameGraphFormat.Rgba8Srgb,
        FrameGraphResourceUsage.ColorAttachment | FrameGraphResourceUsage.Present));

graph.AddPass("Opaque", FrameGraphPassKind.Raster, pass =>
{
    pass.WriteColor(hdr).WriteDepth(depth).Require(FrameGraphTargetFeature.Hdr | FrameGraphTargetFeature.Depth);
});
graph.AddPass("ToneMap", FrameGraphPassKind.PostProcess, pass =>
{
    pass.Read(hdr).WriteColor(output).Require(FrameGraphTargetFeature.ToneMapping);
});

var plan = graph.Compile(FrameGraphQualityProfile.High);
var resized = plan.Resize(new FrameGraphSize(1920, 1080));
var capture = resized.CaptureDiagnostics().ToJson();
```

`FrameGraphCompiler` infers read-after-write and write-after-read dependencies, emits deterministic topological order, resolves resource lifetimes, and records state transitions. `CpuReferenceFrameGraphTarget` validates the capability contract and captures the accepted plan without claiming ownership of scene compilation or shading.

## API

| Type | Role |
|------|------|
| `FrameGraphBuilder` | Creates typed resources and pass descriptions |
| `FrameGraphTextureDescription` / `FrameGraphBufferDescription` | Resource format, usage, lifetime, and resize policy |
| `FrameGraphAttachment` | Typed color/depth attachment metadata |
| `FrameGraphCompiler` / `CompiledFrameGraph` | Dependency ordering, resource lifetimes, resize, and barriers |
| `FrameGraphQualityProfile` | Render scale and HDR/depth/emission/bloom/tone mapping policy |
| `FrameGraphTargetCapabilities` | Neutral target capability declaration |
| `CpuReferenceFrameGraphTarget` | Headless validation and deterministic capture target |
| `FrameGraphDiagnosticGraph` | Stable text and JSON graph capture |

## Related

| Package | Role |
|---------|------|
| `Novolis.Rendering.Scene` | Authoring scene data |
| `Novolis.Rendering.Compile` | Existing `SceneCompiler` authority |
| `Novolis.Rendering.Runtime` | Existing backend and compiled-scene contracts |
| `Novolis.Rendering.Abstractions` | Host-neutral CPU image buffers |

## More documentation

- [Frame graph design](../../docs/frame-graph.md)
- [Materials and backends](../../docs/materials-and-backends.md)
