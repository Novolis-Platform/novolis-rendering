namespace Novolis.Rendering.FrameGraph;

/// <summary>Common format helpers used by validation and target declarations.</summary>
public static class FrameGraphFormatExtensions
{
    /// <summary>Returns whether the format is a depth or depth-stencil format.</summary>
    public static bool IsDepth(this FrameGraphFormat format) =>
        format is FrameGraphFormat.D16Unorm
            or FrameGraphFormat.D24UnormS8Uint
            or FrameGraphFormat.D32Float
            or FrameGraphFormat.D32FloatS8Uint;

    /// <summary>Returns whether the format carries a floating-point HDR range.</summary>
    public static bool IsHdr(this FrameGraphFormat format) =>
        format is FrameGraphFormat.R16Float
            or FrameGraphFormat.Rg16Float
            or FrameGraphFormat.Rgba16Float
            or FrameGraphFormat.R32Float
            or FrameGraphFormat.Rg32Float
            or FrameGraphFormat.Rgba32Float;

    /// <summary>Returns the conventional byte width for one format element.</summary>
    public static int BytesPerElement(this FrameGraphFormat format) =>
        format switch
        {
            FrameGraphFormat.R8Unorm => 1,
            FrameGraphFormat.Rg8Unorm => 2,
            FrameGraphFormat.Rgba8Unorm or FrameGraphFormat.Rgba8Srgb => 4,
            FrameGraphFormat.R16Float => 2,
            FrameGraphFormat.Rg16Float => 4,
            FrameGraphFormat.Rgba16Float => 8,
            FrameGraphFormat.R32Float => 4,
            FrameGraphFormat.Rg32Float => 8,
            FrameGraphFormat.Rgba32Float => 16,
            FrameGraphFormat.D16Unorm => 2,
            FrameGraphFormat.D24UnormS8Uint or FrameGraphFormat.D32Float => 4,
            FrameGraphFormat.D32FloatS8Uint => 8,
            _ => 0,
        };
}
