namespace Novolis.Rendering.FrameGraph;

/// <summary>States whether a target description claims executable rendering.</summary>
public enum FrameGraphTargetExecutionMode
{
    /// <summary>Only validates and captures graph contracts.</summary>
    ValidationOnly,

    /// <summary>Describes a neutral program consumed by another host.</summary>
    DescriptionOnly,

    /// <summary>Describes a deterministic reference/bake contract.</summary>
    ReferenceOnly,

    /// <summary>Execution is owned by a path-tracing backend.</summary>
    PathTrace,

    /// <summary>Execution is owned by a raster host.</summary>
    Raster,

    /// <summary>Execution is owned by a planar host.</summary>
    Planar,

    /// <summary>Application-defined execution mode.</summary>
    Custom,
}
