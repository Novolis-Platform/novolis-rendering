namespace Novolis.Rendering.FrameGraph;

/// <summary>Pixel and attachment formats understood by the neutral frame graph.</summary>
public enum FrameGraphFormat
{
    /// <summary>No format declared.</summary>
    Unknown = 0,
    /// <summary>Single-channel eight-bit normalized format.</summary>
    R8Unorm,
    /// <summary>Two-channel eight-bit normalized format.</summary>
    Rg8Unorm,
    /// <summary>Four-channel eight-bit normalized format.</summary>
    Rgba8Unorm,
    /// <summary>Four-channel eight-bit sRGB format.</summary>
    Rgba8Srgb,
    /// <summary>Single-channel sixteen-bit floating-point format.</summary>
    R16Float,
    /// <summary>Two-channel sixteen-bit floating-point format.</summary>
    Rg16Float,
    /// <summary>Four-channel sixteen-bit floating-point format.</summary>
    Rgba16Float,
    /// <summary>Single-channel thirty-two-bit floating-point format.</summary>
    R32Float,
    /// <summary>Two-channel thirty-two-bit floating-point format.</summary>
    Rg32Float,
    /// <summary>Four-channel thirty-two-bit floating-point format.</summary>
    Rgba32Float,
    /// <summary>Sixteen-bit normalized depth format.</summary>
    D16Unorm,
    /// <summary>Twenty-four-bit depth and eight-bit stencil format.</summary>
    D24UnormS8Uint,
    /// <summary>Thirty-two-bit floating-point depth format.</summary>
    D32Float,
    /// <summary>Thirty-two-bit floating-point depth and eight-bit stencil format.</summary>
    D32FloatS8Uint,
}
