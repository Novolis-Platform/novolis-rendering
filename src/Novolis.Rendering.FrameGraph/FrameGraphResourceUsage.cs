namespace Novolis.Rendering.FrameGraph;

/// <summary>Describes how a graph resource may be used by passes.</summary>
[Flags]
public enum FrameGraphResourceUsage
{
    /// <summary>No usage declared.</summary>
    None = 0,
    /// <summary>Sampled shader read.</summary>
    Sampled = 1 << 0,
    /// <summary>Storage shader read.</summary>
    StorageRead = 1 << 1,
    /// <summary>Storage shader write.</summary>
    StorageWrite = 1 << 2,
    /// <summary>Color attachment access.</summary>
    ColorAttachment = 1 << 3,
    /// <summary>Depth or stencil attachment access.</summary>
    DepthStencilAttachment = 1 << 4,
    /// <summary>Transfer source access.</summary>
    TransferSource = 1 << 5,
    /// <summary>Transfer destination access.</summary>
    TransferDestination = 1 << 6,
    /// <summary>Vertex input access.</summary>
    Vertex = 1 << 7,
    /// <summary>Index input access.</summary>
    Index = 1 << 8,
    /// <summary>Uniform input access.</summary>
    Uniform = 1 << 9,
    /// <summary>Presentation access.</summary>
    Present = 1 << 10,
    /// <summary>Combined storage read and write access.</summary>
    Storage = StorageRead | StorageWrite,
    /// <summary>Combined transfer source and destination access.</summary>
    Transfer = TransferSource | TransferDestination,
}
