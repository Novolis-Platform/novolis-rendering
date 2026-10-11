namespace Novolis.Rendering.FrameGraph;

/// <summary>Controls whether an attachment remains available after a pass.</summary>
public enum FrameGraphStoreOperation
{
    /// <summary>Contents do not need to remain available.</summary>
    DontCare,
    /// <summary>Contents remain available for later accesses.</summary>
    Store,
}
