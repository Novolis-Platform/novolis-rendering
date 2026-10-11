# Renderer-owned frame graph

`Novolis.Rendering.FrameGraph` is the low-level per-frame contract for resources and passes. It is intentionally separate from authoring graphs and from the existing scene path:

```text
Scene → SceneCompiler → CompiledScene → IRayTracingBackend
                                      ↘
                         FrameGraph description → target composition
```

The frame graph does not compile authoring scenes, execute workflow nodes, or own a window or GPU API.

## Resource and attachment contracts

`FrameGraphBuilder.CreateTexture` and `CreateBuffer` return typed handles. Their descriptions declare:

- format and dimensions;
- sampled, storage, attachment, transfer, vertex, index, uniform, and present usage;
- transient, persistent, or imported lifetime;
- fixed, output-sized, or output-scaled resize policy.

`WriteColor`, `ReadColor`, `WriteDepth`, `ReadDepth`, and `Use(FrameGraphAttachment)` retain attachment roles, load/store operations, and clear values without introducing a backend-specific texture type.

## Compilation

`FrameGraphCompiler` validates format/usage compatibility, transient read-before-write hazards, attachment types, explicit pass handles, target requirements, and quality feature policy. It then:

1. adds explicit and resource-derived dependency edges;
2. performs a stable topological sort;
3. resolves first and last resource use;
4. maps accesses to neutral resource states;
5. emits barriers for state transitions.

The compiler never invokes a scene compiler or backend. A target can consume `CompiledFrameGraph.Passes`, `Dependencies`, and `Barriers` from its own execution authority.

## Quality, target, and resize

`FrameGraphQualityProfile` supplies render scale, bloom level count, and feature enablement for HDR, depth, emission, bloom, and tone mapping. `FrameGraphTargetCapabilities` reports the same feature set plus optional dimension limits. Required pass features are validated against both policies.

`CompiledFrameGraph.Resize` recompiles the immutable description for a new output size while retaining topology and policy. Output-sized resources follow the new dimensions; fixed resources retain their declared dimensions.

`CpuReferenceFrameGraphTarget` is a small headless contract target. It checks dimensions and capabilities, accepts a compiled plan, and records a deterministic diagnostic capture. It is a reference for graph validation and tests, not a replacement for `SceneCompiler`, progressive backends, or presentation.

## Deterministic diagnostics

`CompiledFrameGraph.CaptureDiagnostics()` returns `FrameGraphDiagnosticGraph`. Its resource, pass, and barrier rows have stable IDs and ordering. `ToDeterministicText()` is useful for focused golden tests; `ToJson()` is suitable for build reports and graph inspection.

The package has no dependency on UI toolkits, native graphics APIs, simulation, workflow, or visual authoring libraries.
