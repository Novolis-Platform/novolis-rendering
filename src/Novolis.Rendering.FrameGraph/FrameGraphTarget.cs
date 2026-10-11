namespace Novolis.Rendering.FrameGraph;

/// <summary>Neutral capability declaration for a frame graph execution target.</summary>
public sealed record FrameGraphTargetCapabilities
{
    /// <summary>Creates a target capability declaration.</summary>
    public FrameGraphTargetCapabilities(
        FrameGraphTargetFeature features = FrameGraphTargetFeature.All,
        int maxWidth = 0,
        int maxHeight = 0)
        : this(
            "Target",
            features,
            maxWidth,
            maxHeight,
            FrameGraphPipelineStageMask.All,
            FrameGraphTargetKind.Custom,
            FrameGraphTargetExecutionMode.ValidationOnly)
    {
    }

    /// <summary>Creates a named target capability declaration.</summary>
    public FrameGraphTargetCapabilities(
        string name,
        FrameGraphTargetFeature features,
        int maxWidth = 0,
        int maxHeight = 0,
        FrameGraphPipelineStageMask supportedStages = FrameGraphPipelineStageMask.All,
        FrameGraphTargetKind kind = FrameGraphTargetKind.Custom,
        FrameGraphTargetExecutionMode executionMode = FrameGraphTargetExecutionMode.ValidationOnly)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (maxWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxWidth));
        }

        if (maxHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHeight));
        }

        Name = name;
        Features = features;
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
        SupportedStages = supportedStages;
        Kind = kind;
        ExecutionMode = executionMode;
    }

    /// <summary>Target identifier used in diagnostics.</summary>
    public string Name { get; }

    /// <summary>Supported frame graph features.</summary>
    public FrameGraphTargetFeature Features { get; }

    /// <summary>Alias emphasizing that this is a supported-feature declaration.</summary>
    public FrameGraphTargetFeature SupportedFeatures => Features;

    /// <summary>Stages that this target can describe or execute.</summary>
    public FrameGraphPipelineStageMask SupportedStages { get; }

    /// <summary>Target family represented by this declaration.</summary>
    public FrameGraphTargetKind Kind { get; }

    /// <summary>Execution authority represented by this declaration.</summary>
    public FrameGraphTargetExecutionMode ExecutionMode { get; }

    /// <summary>Maximum width, or zero for no declared limit.</summary>
    public int MaxWidth { get; }

    /// <summary>Maximum height, or zero for no declared limit.</summary>
    public int MaxHeight { get; }

    /// <summary>Whether HDR resources and passes are supported.</summary>
    public bool SupportsHdr => Supports(FrameGraphTargetFeature.Hdr);

    /// <summary>Whether depth resources and passes are supported.</summary>
    public bool SupportsDepth => Supports(FrameGraphTargetFeature.Depth);

    /// <summary>Whether emission resources and passes are supported.</summary>
    public bool SupportsEmission => Supports(FrameGraphTargetFeature.Emission);

    /// <summary>Whether bloom resources and passes are supported.</summary>
    public bool SupportsBloom => Supports(FrameGraphTargetFeature.Bloom);

    /// <summary>Whether tone mapping resources and passes are supported.</summary>
    public bool SupportsToneMapping => Supports(FrameGraphTargetFeature.ToneMapping);

    /// <summary>Short alias for tone mapping capability checks.</summary>
    public bool SupportsToneMap => SupportsToneMapping;

    /// <summary>Whether physically based material evaluation is supported.</summary>
    public bool SupportsPbr => Supports(FrameGraphTargetFeature.Pbr);

    /// <summary>Whether shadow evaluation is supported.</summary>
    public bool SupportsShadows => Supports(FrameGraphTargetFeature.Shadows);

    /// <summary>Whether transparent composition is supported.</summary>
    public bool SupportsTransparency => Supports(FrameGraphTargetFeature.Transparency);

    /// <summary>Whether particle composition is supported.</summary>
    public bool SupportsParticles => Supports(FrameGraphTargetFeature.Particles);

    /// <summary>Whether decal composition is supported.</summary>
    public bool SupportsDecals => Supports(FrameGraphTargetFeature.Decals);

    /// <summary>Whether volumetric integration is supported.</summary>
    public bool SupportsVolumetrics => Supports(FrameGraphTargetFeature.Volumetrics);

    /// <summary>Whether color grading is supported.</summary>
    public bool SupportsColorGrading => Supports(FrameGraphTargetFeature.ColorGrading);

    /// <summary>Whether vignette processing is supported.</summary>
    public bool SupportsVignette => Supports(FrameGraphTargetFeature.Vignette);

    /// <summary>Whether distortion processing is supported.</summary>
    public bool SupportsDistortion => Supports(FrameGraphTargetFeature.Distortion);

    /// <summary>Whether diagnostic overlays are supported.</summary>
    public bool SupportsDiagnostics => Supports(FrameGraphTargetFeature.Diagnostics);

    /// <summary>Whether host presentation is supported.</summary>
    public bool SupportsPresentation => Supports(FrameGraphTargetFeature.Presentation);

    /// <summary>Returns whether all requested features are supported.</summary>
    public bool Supports(FrameGraphTargetFeature features) =>
        (Features & features) == features;

    /// <summary>Returns whether a pipeline stage is supported.</summary>
    public bool SupportsStage(FrameGraphPipelineStage stage) =>
        (SupportedStages & stage.ToMask()) == stage.ToMask();

    /// <summary>Returns whether the target accepts the dimensions.</summary>
    public bool SupportsSize(FrameGraphSize size) =>
        (MaxWidth == 0 || size.Width <= MaxWidth)
        && (MaxHeight == 0 || size.Height <= MaxHeight);

    /// <summary>Capabilities advertised by the headless CPU graph reference.</summary>
    public static FrameGraphTargetCapabilities CpuReference { get; } =
        new(
            "CPU Reference",
            FrameGraphTargetFeature.All,
            supportedStages: FrameGraphPipelineStageMask.All,
            kind: FrameGraphTargetKind.CpuReference,
            executionMode: FrameGraphTargetExecutionMode.ValidationOnly);

    /// <summary>Capabilities for the deterministic CPU path-trace/bake reference.</summary>
    public static FrameGraphTargetCapabilities CpuBake { get; } =
        new(
            "CPU Bake/Reference",
            FrameGraphTargetFeature.Pbr
                | FrameGraphTargetFeature.Emission
                | FrameGraphTargetFeature.Shadows
                | FrameGraphTargetFeature.Transparency,
            supportedStages: FrameGraphPipelineStageMask.Preparation
                | FrameGraphPipelineStageMask.OpaqueGeometry
                | FrameGraphPipelineStageMask.HdrOpaqueLighting
                | FrameGraphPipelineStageMask.Emission
                | FrameGraphPipelineStageMask.Transparency,
            kind: FrameGraphTargetKind.CpuBake,
            executionMode: FrameGraphTargetExecutionMode.ReferenceOnly);

    /// <summary>Capabilities implemented by the existing CPU ray-tracing backend.</summary>
    public static FrameGraphTargetCapabilities PathTraceCpu { get; } =
        new(
            "CPU Path Trace",
            CpuBake.Features,
            supportedStages: CpuBake.SupportedStages,
            kind: FrameGraphTargetKind.PathTrace,
            executionMode: FrameGraphTargetExecutionMode.PathTrace);

    /// <summary>Capabilities implemented by the existing ILGPU path-tracing backend.</summary>
    public static FrameGraphTargetCapabilities PathTraceIlgpu { get; } =
        new(
            "ILGPU Path Trace",
            PathTraceCpu.Features,
            supportedStages: PathTraceCpu.SupportedStages,
            kind: FrameGraphTargetKind.PathTrace,
            executionMode: FrameGraphTargetExecutionMode.PathTrace);

    /// <summary>Capabilities of the current Vulkan-named CPU fallback backend.</summary>
    public static FrameGraphTargetCapabilities PathTraceVulkanFallback { get; } =
        new(
            "Vulkan Path Trace (CPU fallback)",
            PathTraceCpu.Features,
            supportedStages: PathTraceCpu.SupportedStages,
            kind: FrameGraphTargetKind.PathTrace,
            executionMode: FrameGraphTargetExecutionMode.PathTrace);

    /// <summary>Alias for the CPU path-trace capability declaration.</summary>
    public static FrameGraphTargetCapabilities PathTrace => PathTraceCpu;

    /// <summary>Neutral raster program declaration with no backend claims.</summary>
    public static FrameGraphTargetCapabilities RasterNeutral { get; } =
        new(
            "Raster (neutral program)",
            FrameGraphTargetFeature.None,
            supportedStages: FrameGraphPipelineStageMask.Preparation
                | FrameGraphPipelineStageMask.DepthVisibility
                | FrameGraphPipelineStageMask.OpaqueGeometry
                | FrameGraphPipelineStageMask.DiagnosticsAndPresentation,
            kind: FrameGraphTargetKind.Raster,
            executionMode: FrameGraphTargetExecutionMode.DescriptionOnly);

    /// <summary>Neutral planar draw-list declaration with no host claims.</summary>
    public static FrameGraphTargetCapabilities PlanarNeutral { get; } =
        new(
            "Planar (neutral draw list)",
            FrameGraphTargetFeature.None,
            supportedStages: FrameGraphPipelineStageMask.Preparation
                | FrameGraphPipelineStageMask.OpaqueGeometry
                | FrameGraphPipelineStageMask.DiagnosticsAndPresentation,
            kind: FrameGraphTargetKind.Planar,
            executionMode: FrameGraphTargetExecutionMode.DescriptionOnly);

    /// <summary>Alias for the neutral raster capability declaration.</summary>
    public static FrameGraphTargetCapabilities Raster => RasterNeutral;

    /// <summary>Alias for the neutral planar capability declaration.</summary>
    public static FrameGraphTargetCapabilities Planar => PlanarNeutral;

    /// <summary>Unbounded default capabilities for a generic validation target.</summary>
    public static FrameGraphTargetCapabilities Default { get; } =
        new(
            "Target",
            FrameGraphTargetFeature.All,
            supportedStages: FrameGraphPipelineStageMask.All,
            kind: FrameGraphTargetKind.Custom,
            executionMode: FrameGraphTargetExecutionMode.ValidationOnly);
}
