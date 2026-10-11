namespace Novolis.Rendering.FrameGraph;

/// <summary>Describes one structured buffer resource in a frame graph.</summary>
public readonly record struct FrameGraphBufferDescription(
    int SizeInBytes,
    FrameGraphResourceUsage Usage,
    FrameGraphResourceLifetime Lifetime = FrameGraphResourceLifetime.Transient,
    int StrideInBytes = 0);
