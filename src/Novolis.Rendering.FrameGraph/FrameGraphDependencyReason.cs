namespace Novolis.Rendering.FrameGraph;

/// <summary>Reason an ordering edge exists between two passes.</summary>
[Flags]
public enum FrameGraphDependencyReason
{
    /// <summary>No dependency reason.</summary>
    None = 0,
    /// <summary>Dependency explicitly requested by the pass author.</summary>
    Explicit = 1 << 0,
    /// <summary>Read follows a prior write.</summary>
    ResourceReadAfterWrite = 1 << 1,
    /// <summary>Write follows a prior read.</summary>
    ResourceWriteAfterRead = 1 << 2,
    /// <summary>Write follows a prior write.</summary>
    ResourceWriteAfterWrite = 1 << 3,
}
