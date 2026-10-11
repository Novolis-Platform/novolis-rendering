namespace Novolis.Rendering.FrameGraph;

/// <summary>Controls when a graph resource is allowed to be reclaimed or retained.</summary>
public enum FrameGraphResourceLifetime
{
    /// <summary>May be reclaimed after its last pass.</summary>
    Transient,
    /// <summary>Retained across frame executions.</summary>
    Persistent,
    /// <summary>Owned and initialized by an external producer.</summary>
    Imported,
}
