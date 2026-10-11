namespace Novolis.Rendering.FrameGraph;

/// <summary>Controls how an attachment starts a pass.</summary>
public enum FrameGraphLoadOperation
{
    /// <summary>Prior contents are not needed.</summary>
    DontCare,
    /// <summary>Prior contents are preserved.</summary>
    Load,
    /// <summary>Prior contents are replaced by a clear value.</summary>
    Clear,
}
