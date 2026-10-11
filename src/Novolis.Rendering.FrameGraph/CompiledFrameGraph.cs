namespace Novolis.Rendering.FrameGraph;

/// <summary>Compiled renderer-owned graph with ordering, lifetime, and barrier data.</summary>
public sealed class CompiledFrameGraph
{
    private readonly IReadOnlyDictionary<FrameGraphResourceHandle, FrameGraphCompiledResource> _resources;

    internal CompiledFrameGraph(
        FrameGraphDescription description,
        FrameGraphSize outputSize,
        FrameGraphSize renderSize,
        FrameGraphQualityProfile qualityProfile,
        FrameGraphTargetCapabilities? targetCapabilities,
        FrameGraphTargetDescription? targetDescription,
        IReadOnlyList<FrameGraphCompiledResource> resources,
        IReadOnlyList<FrameGraphCompiledPass> passes,
        IReadOnlyList<FrameGraphDependency> dependencies,
        IReadOnlyList<FrameGraphBarrier> barriers,
        bool includeInitialTransitions)
    {
        Description = description;
        OutputSize = outputSize;
        RenderSize = renderSize;
        QualityProfile = qualityProfile;
        TargetCapabilities = targetCapabilities;
        TargetDescription = targetDescription;
        Resources = resources;
        Passes = passes;
        Dependencies = dependencies;
        Barriers = barriers;
        IncludeInitialTransitions = includeInitialTransitions;
        _resources = resources.ToDictionary(resource => resource.Description.Handle);
    }

    /// <summary>Source graph description retained for resize and diagnostics.</summary>
    public FrameGraphDescription Description { get; }

    /// <summary>External output size.</summary>
    public FrameGraphSize OutputSize { get; }

    /// <summary>Quality-scaled render size.</summary>
    public FrameGraphSize RenderSize { get; }

    /// <summary>Quality policy used by this plan.</summary>
    public FrameGraphQualityProfile QualityProfile { get; }

    /// <summary>Target capabilities used during compilation, when supplied.</summary>
    public FrameGraphTargetCapabilities? TargetCapabilities { get; }

    /// <summary>Typed target description used during compilation, when supplied.</summary>
    public FrameGraphTargetDescription? TargetDescription { get; }

    /// <summary>Whether initial state transitions are present in <see cref="Barriers"/>.</summary>
    public bool IncludeInitialTransitions { get; }

    /// <summary>Resources with dimensions and first/last use resolved.</summary>
    public IReadOnlyList<FrameGraphCompiledResource> Resources { get; }

    /// <summary>Passes in deterministic topological order.</summary>
    public IReadOnlyList<FrameGraphCompiledPass> Passes { get; }

    /// <summary>All dependency edges in deterministic order.</summary>
    public IReadOnlyList<FrameGraphDependency> Dependencies { get; }

    /// <summary>All state transitions in execution order.</summary>
    public IReadOnlyList<FrameGraphBarrier> Barriers { get; }

    /// <summary>Gets a compiled resource by neutral handle.</summary>
    public FrameGraphCompiledResource GetResource(FrameGraphResourceHandle handle) =>
        _resources.TryGetValue(handle, out var resource)
            ? resource
            : throw new KeyNotFoundException($"Unknown graph resource {handle}.");

    /// <summary>Gets a compiled texture by typed handle.</summary>
    public FrameGraphCompiledResource GetResource(FrameGraphTextureHandle handle) =>
        GetResource((FrameGraphResourceHandle)handle);

    /// <summary>Gets a compiled buffer by typed handle.</summary>
    public FrameGraphCompiledResource GetResource(FrameGraphBufferHandle handle) =>
        GetResource((FrameGraphResourceHandle)handle);

    /// <summary>Captures a deterministic diagnostic representation.</summary>
    public FrameGraphDiagnosticGraph CaptureDiagnostics() =>
        FrameGraphDiagnosticGraph.Capture(this);

    /// <summary>Recompiles the same topology for a new output size.</summary>
    public CompiledFrameGraph Resize(FrameGraphSize outputSize) =>
        FrameGraphCompiler.Compile(
            Description.WithOutputSize(outputSize),
            new FrameGraphCompileOptions
            {
                QualityProfile = QualityProfile,
                TargetCapabilities = TargetCapabilities,
                TargetDescription = TargetDescription,
                IncludeInitialTransitions = IncludeInitialTransitions,
            });
}
