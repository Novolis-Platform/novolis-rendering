using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;
using StbImageSharp;

namespace Novolis.Rendering.Backends.TwoD.Silk;

/// <summary>Loads PNG files into a <see cref="TwoDTextureRegistry"/>.</summary>
public static class SilkTwoDPngLoader
{
    /// <summary>Loads a PNG from disk and registers RGBA pixels.</summary>
    /// <param name="registry">Target catalog.</param>
    /// <param name="path">File path.</param>
    /// <returns>Registered texture id.</returns>
    public static TwoDTextureId LoadPng(TwoDTextureRegistry registry, string path)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrEmpty(path);
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        var pixels = new Rgba32[image.Width * image.Height];
        var src = image.Data;
        for (var i = 0; i < pixels.Length; i++)
        {
            var o = i * 4;
            pixels[i] = new Rgba32(src[o], src[o + 1], src[o + 2], src[o + 3]);
        }

        return registry.Register(pixels, image.Width, image.Height, Path.GetFileName(path));
    }
}
