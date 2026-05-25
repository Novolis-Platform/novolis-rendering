using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Presentation.Abstractions;
using Silk.NET.OpenGL;
using StbImageWriteSharp;

namespace Novolis.Rendering.Backends.TwoD.Silk;

/// <summary>Reads the current OpenGL framebuffer into CPU pixels or PNG.</summary>
public static class SilkTwoDFramebufferCapture
{
    /// <summary>Reads RGBA pixels from the bound framebuffer (origin bottom-left, flipped to top-left).</summary>
    public static bool TryReadPixels(GL gl, int width, int height, out Rgba32[] pixels)
    {
        pixels = [];
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var raw = new byte[width * height * 4];
        unsafe
        {
            fixed (byte* p = raw)
            {
                gl.ReadPixels(0, 0, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, p);
            }
        }

        pixels = new Rgba32[width * height];
        for (var y = 0; y < height; y++)
        {
            var srcRow = (height - 1 - y) * width * 4;
            for (var x = 0; x < width; x++)
            {
                var i = srcRow + x * 4;
                pixels[y * width + x] = new Rgba32(raw[i], raw[i + 1], raw[i + 2], raw[i + 3]);
            }
        }

        return true;
    }

    /// <summary>Wraps a freshly read framebuffer as <see cref="IRenderOutput"/>.</summary>
    public static ImageBufferRenderOutput ToRenderOutput(GL gl, int width, int height)
    {
        if (!TryReadPixels(gl, width, height, out var pixels))
        {
            return new ImageBufferRenderOutput();
        }

        var buffer = new ImageBuffer(width, height);
        pixels.AsSpan().CopyTo(buffer.AsSpan());
        return new ImageBufferRenderOutput { Buffer = buffer };
    }

    /// <summary>Encodes the current framebuffer as PNG bytes.</summary>
    public static byte[] EncodePng(GL gl, int width, int height)
    {
        if (!TryReadPixels(gl, width, height, out var pixels))
        {
            return [];
        }

        var rgba = new byte[pixels.Length * 4];
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var o = i * 4;
            rgba[o] = p.R;
            rgba[o + 1] = p.G;
            rgba[o + 2] = p.B;
            rgba[o + 3] = p.A;
        }

        using var ms = new MemoryStream();
        ImageWriter pngWriter = new();
        pngWriter.WritePng(rgba, width, height, ColorComponents.RedGreenBlueAlpha, ms);
        return ms.ToArray();
    }

    /// <summary>Captures one frame for streaming sessions.</summary>
    public static SilkTwoDCapturedFrame CaptureFrame(GL gl, int width, int height, int frameIndex, TimeSpan elapsed) =>
        new()
        {
            FrameIndex = frameIndex,
            Width = width,
            Height = height,
            Png = EncodePng(gl, width, height),
            Elapsed = elapsed,
        };
}
