namespace Novolis.Rendering.FrameGraph;

/// <summary>Identifies the kind of target a frame graph describes.</summary>
public enum FrameGraphTargetKind
{
    /// <summary>Headless CPU graph validation and deterministic capture.</summary>
    CpuReference,

    /// <summary>Deterministic CPU bake/reference contract.</summary>
    CpuBake,

    /// <summary>Path-trace target backed by an existing ray-tracing backend.</summary>
    PathTrace,

    /// <summary>Host-neutral raster target program.</summary>
    Raster,

    /// <summary>Host-neutral planar target program.</summary>
    Planar,

    /// <summary>Application-defined target.</summary>
    Custom,
}
