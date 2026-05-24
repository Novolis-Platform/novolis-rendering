namespace Novolis.Rendering.TwoD;

/// <summary>Metadata for a registered texture.</summary>
/// <param name="Id">Texture handle.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="Name">Optional debug name (file name or label).</param>
public readonly record struct TwoDTextureInfo(TwoDTextureId Id, int Width, int Height, string? Name);
