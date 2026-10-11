namespace Novolis.Rendering.FrameGraph;

/// <summary>Immutable quality and feature policy applied when compiling a graph.</summary>
public sealed record FrameGraphQualityProfile
{
    /// <summary>Creates a quality profile.</summary>
    public FrameGraphQualityProfile(
        string name,
        float renderScale = 1f,
        FrameGraphTargetFeature enabledFeatures = FrameGraphTargetFeature.All,
        int bloomLevels = 5,
        FrameGraphQualityTier tier = FrameGraphQualityTier.Custom,
        FrameGraphPipelineStageMask enabledStages = FrameGraphPipelineStageMask.All)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (!float.IsFinite(renderScale) || renderScale <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(renderScale));
        }

        if (bloomLevels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bloomLevels));
        }

        if ((enabledStages & ~FrameGraphPipelineStageMask.All) != FrameGraphPipelineStageMask.None)
        {
            throw new ArgumentOutOfRangeException(nameof(enabledStages));
        }

        Name = name;
        RenderScale = renderScale;
        EnabledFeatures = enabledFeatures;
        BloomLevels = bloomLevels;
        Tier = tier;
        EnabledStages = enabledStages;
    }

    /// <summary>Human-readable profile name.</summary>
    public string Name { get; }

    /// <summary>Scale applied to output-sized resources.</summary>
    public float RenderScale { get; }

    /// <summary>Features enabled for this compile.</summary>
    public FrameGraphTargetFeature EnabledFeatures { get; }

    /// <summary>Requested bloom pyramid level count.</summary>
    public int BloomLevels { get; }

    /// <summary>Named quality tier.</summary>
    public FrameGraphQualityTier Tier { get; }

    /// <summary>Stages allowed by this quality profile.</summary>
    public FrameGraphPipelineStageMask EnabledStages { get; }

    /// <summary>Returns whether every requested feature is enabled.</summary>
    public bool IsEnabled(FrameGraphTargetFeature feature) =>
        (EnabledFeatures & feature) == feature;

    /// <summary>Returns whether a pipeline stage is enabled.</summary>
    public bool IsStageEnabled(FrameGraphPipelineStage stage) =>
        (EnabledStages & stage.ToMask()) == stage.ToMask();

    /// <summary>Default full-feature profile for reference and high-quality output.</summary>
    public static FrameGraphQualityProfile Default { get; } =
        new("Default", 1f, FrameGraphTargetFeature.All, 5, FrameGraphQualityTier.High);

    /// <summary>Lower-cost profile that keeps depth and tone mapping.</summary>
    public static FrameGraphQualityProfile Low { get; } =
        new(
            "Low",
            0.5f,
            FrameGraphTargetFeature.Depth
                | FrameGraphTargetFeature.Pbr
                | FrameGraphTargetFeature.ToneMapping,
            2,
            FrameGraphQualityTier.Low);

    /// <summary>Balanced profile for interactive previews.</summary>
    public static FrameGraphQualityProfile Medium { get; } =
        new(
            "Medium",
            0.75f,
            FrameGraphTargetFeature.Hdr
                | FrameGraphTargetFeature.Depth
                | FrameGraphTargetFeature.Emission
                | FrameGraphTargetFeature.Pbr
                | FrameGraphTargetFeature.Shadows
                | FrameGraphTargetFeature.Transparency
                | FrameGraphTargetFeature.ToneMapping,
            3,
            FrameGraphQualityTier.Medium);

    /// <summary>High-quality profile with the complete lighting feature set.</summary>
    public static FrameGraphQualityProfile High { get; } =
        new("High", 1f, FrameGraphTargetFeature.All, 5, FrameGraphQualityTier.High);

    /// <summary>Highest quality profile exposed by the foundation.</summary>
    public static FrameGraphQualityProfile Ultra { get; } =
        new("Ultra", 1f, FrameGraphTargetFeature.All, 6, FrameGraphQualityTier.Ultra);
}
