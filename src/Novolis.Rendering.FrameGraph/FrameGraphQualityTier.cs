namespace Novolis.Rendering.FrameGraph;

/// <summary>Describes the broad quality tier selected for a frame graph.</summary>
public enum FrameGraphQualityTier
{
    /// <summary>Lowest-cost profile.</summary>
    Low,
    /// <summary>Balanced interactive profile.</summary>
    Medium,
    /// <summary>High-quality profile.</summary>
    High,
    /// <summary>Highest-quality profile.</summary>
    Ultra,
    /// <summary>Application-defined profile.</summary>
    Custom,
}
