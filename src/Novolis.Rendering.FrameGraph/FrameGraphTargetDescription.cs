namespace Novolis.Rendering.FrameGraph;

/// <summary>
/// Host-neutral description of a frame graph target.
/// </summary>
/// <remarks>
/// A description is a capability and execution-boundary contract. It does not
/// create a graphics device, invoke a backend, or claim support that is not
/// represented by its capabilities.
/// </remarks>
public sealed record FrameGraphTargetDescription
{
    /// <summary>Creates a target description.</summary>
    public FrameGraphTargetDescription(
        string id,
        string name,
        FrameGraphTargetCapabilities capabilities)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(capabilities);

        Id = id;
        Name = name;
        Capabilities = capabilities;
    }

    /// <summary>Stable target identifier for manifests and diagnostics.</summary>
    public string Id { get; }

    /// <summary>Human-readable target name.</summary>
    public string Name { get; }

    /// <summary>Target feature and stage declaration.</summary>
    public FrameGraphTargetCapabilities Capabilities { get; }

    /// <summary>Target family.</summary>
    public FrameGraphTargetKind Kind => Capabilities.Kind;

    /// <summary>Execution authority represented by the target.</summary>
    public FrameGraphTargetExecutionMode ExecutionMode => Capabilities.ExecutionMode;

    /// <summary>Whether this target description claims to execute rendering work.</summary>
    public bool ClaimsExecution =>
        ExecutionMode is not FrameGraphTargetExecutionMode.ValidationOnly
            and not FrameGraphTargetExecutionMode.DescriptionOnly
            and not FrameGraphTargetExecutionMode.ReferenceOnly;

    /// <summary>Headless validation and graph-capture target.</summary>
    public static FrameGraphTargetDescription CpuReference { get; } =
        new("cpu-reference", "CPU Reference", FrameGraphTargetCapabilities.CpuReference);

    /// <summary>Deterministic CPU bake/reference contract.</summary>
    public static FrameGraphTargetDescription CpuBake { get; } =
        new("cpu-bake", "CPU Bake/Reference", FrameGraphTargetCapabilities.CpuBake);

    /// <summary>Existing CPU path-tracing backend contract.</summary>
    public static FrameGraphTargetDescription PathTraceCpu { get; } =
        new("path-trace-cpu", "CPU Path Trace", FrameGraphTargetCapabilities.PathTraceCpu);

    /// <summary>Existing ILGPU path-tracing backend contract.</summary>
    public static FrameGraphTargetDescription PathTraceIlgpu { get; } =
        new("path-trace-ilgpu", "ILGPU Path Trace", FrameGraphTargetCapabilities.PathTraceIlgpu);

    /// <summary>Current Vulkan-named CPU fallback contract.</summary>
    public static FrameGraphTargetDescription PathTraceVulkanFallback { get; } =
        new(
            "path-trace-vulkan-cpu",
            "Vulkan Path Trace (CPU fallback)",
            FrameGraphTargetCapabilities.PathTraceVulkanFallback);

    /// <summary>Alias for the CPU path-trace target.</summary>
    public static FrameGraphTargetDescription PathTrace => PathTraceCpu;

    /// <summary>Host-neutral raster program contract.</summary>
    public static FrameGraphTargetDescription RasterNeutral { get; } =
        new("raster-neutral", "Raster (neutral program)", FrameGraphTargetCapabilities.RasterNeutral);

    /// <summary>Host-neutral planar draw-list contract.</summary>
    public static FrameGraphTargetDescription PlanarNeutral { get; } =
        new("planar-neutral", "Planar (neutral draw list)", FrameGraphTargetCapabilities.PlanarNeutral);

    /// <summary>Alias for the neutral raster target.</summary>
    public static FrameGraphTargetDescription Raster => RasterNeutral;

    /// <summary>Alias for the neutral planar target.</summary>
    public static FrameGraphTargetDescription Planar => PlanarNeutral;

    /// <summary>Unbounded custom validation target.</summary>
    public static FrameGraphTargetDescription Default { get; } =
        new("target", "Target", FrameGraphTargetCapabilities.Default);
}
