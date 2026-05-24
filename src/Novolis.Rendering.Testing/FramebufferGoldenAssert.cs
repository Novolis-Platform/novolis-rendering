using System.Security.Cryptography;
using Novolis.Math.Geometry;
using Novolis.Rendering.Presentation.Abstractions;

namespace Novolis.Rendering.Testing;

/// <summary>Golden PNG SHA256 assertions over CPU render output (no native window).</summary>
public static class FramebufferGoldenAssert
{
    public static string Sha256Hex(ReadOnlySpan<Rgba32> pixels)
    {
        var bytes = new byte[pixels.Length * 4];
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            bytes[i * 4] = p.R;
            bytes[i * 4 + 1] = p.G;
            bytes[i * 4 + 2] = p.B;
            bytes[i * 4 + 3] = p.A;
        }

        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    public static string Sha256Hex(IRenderOutput output)
    {
        if (!output.TryGetCpuPixels(out var pixels, out _, out _))
        {
            throw new InvalidOperationException("Output has no CPU pixels.");
        }

        return Sha256Hex(pixels);
    }
}
