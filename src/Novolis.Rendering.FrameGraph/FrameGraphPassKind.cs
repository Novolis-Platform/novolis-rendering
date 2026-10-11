namespace Novolis.Rendering.FrameGraph;

/// <summary>Classifies a pass for diagnostics and target policy.</summary>
public enum FrameGraphPassKind
{
    /// <summary>Camera, scene, or visibility preparation.</summary>
    Preparation,
    /// <summary>Raster or attachment-producing pass.</summary>
    Raster,
    /// <summary>Compute or general shader pass.</summary>
    Compute,
    /// <summary>Copy or transfer pass.</summary>
    Copy,
    /// <summary>Multisample or format resolve pass.</summary>
    Resolve,
    /// <summary>Post-processing pass.</summary>
    PostProcess,
    /// <summary>Host presentation pass.</summary>
    Present,
    /// <summary>Diagnostic or capture pass.</summary>
    Diagnostic,
}
