namespace Novolis.Rendering.FrameGraph;

/// <summary>Controls how a texture responds to output resize.</summary>
public enum FrameGraphSizeMode
{
    /// <summary>Use the declared dimensions unchanged.</summary>
    Fixed,
    /// <summary>Follow the graph output dimensions.</summary>
    Output,
    /// <summary>Follow the graph output dimensions and scale.</summary>
    Scaled,
}
