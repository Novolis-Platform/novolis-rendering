namespace Novolis.Rendering.FrameGraph;

/// <summary>Describes one texture resource in a frame graph.</summary>
public readonly record struct FrameGraphTextureDescription(
    FrameGraphSize Size,
    FrameGraphFormat Format,
    FrameGraphResourceUsage Usage,
    FrameGraphResourceLifetime Lifetime = FrameGraphResourceLifetime.Transient,
    FrameGraphSizeMode SizeMode = FrameGraphSizeMode.Fixed,
    float Scale = 1f,
    int MipLevels = 1,
    int SampleCount = 1)
{
    /// <summary>Creates an output-sized texture description.</summary>
    public static FrameGraphTextureDescription Output(
        FrameGraphFormat format,
        FrameGraphResourceUsage usage,
        FrameGraphResourceLifetime lifetime = FrameGraphResourceLifetime.Transient,
        float scale = 1f,
        int mipLevels = 1,
        int sampleCount = 1) =>
        new(new FrameGraphSize(1, 1), format, usage, lifetime, FrameGraphSizeMode.Output, scale, mipLevels, sampleCount);

    /// <summary>Creates a texture description scaled from the graph output.</summary>
    public static FrameGraphTextureDescription Scaled(
        FrameGraphFormat format,
        FrameGraphResourceUsage usage,
        float scale,
        FrameGraphResourceLifetime lifetime = FrameGraphResourceLifetime.Transient,
        int mipLevels = 1,
        int sampleCount = 1) =>
        new(new FrameGraphSize(1, 1), format, usage, lifetime, FrameGraphSizeMode.Scaled, scale, mipLevels, sampleCount);

    /// <summary>Resolves the physical size for a graph output and quality scale.</summary>
    public FrameGraphSize ResolveSize(FrameGraphSize outputSize, float qualityScale)
    {
        var baseSize = SizeMode switch
        {
            FrameGraphSizeMode.Fixed => Size,
            FrameGraphSizeMode.Output => outputSize.Scale(Scale),
            FrameGraphSizeMode.Scaled => outputSize.Scale(Scale),
            _ => throw new InvalidOperationException($"Unknown size mode {SizeMode}."),
        };

        return SizeMode == FrameGraphSizeMode.Fixed ? baseSize : baseSize.Scale(qualityScale);
    }
}
