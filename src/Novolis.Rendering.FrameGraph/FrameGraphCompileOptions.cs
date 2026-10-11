namespace Novolis.Rendering.FrameGraph;

/// <summary>Options controlling graph compilation and barrier emission.</summary>
public sealed class FrameGraphCompileOptions
{
    /// <summary>Optional output-size override for this compile.</summary>
    public FrameGraphSize? OutputSize { get; init; }

    /// <summary>Quality and feature policy.</summary>
    public FrameGraphQualityProfile QualityProfile { get; init; } = FrameGraphQualityProfile.Default;

    /// <summary>Optional target capability declaration used during validation.</summary>
    public FrameGraphTargetCapabilities? TargetCapabilities { get; init; }

    /// <summary>Optional typed target description used during validation and capture.</summary>
    public FrameGraphTargetDescription? TargetDescription { get; init; }

    /// <summary>Whether transitions from external or undefined state are retained.</summary>
    public bool IncludeInitialTransitions { get; init; } = true;
}
