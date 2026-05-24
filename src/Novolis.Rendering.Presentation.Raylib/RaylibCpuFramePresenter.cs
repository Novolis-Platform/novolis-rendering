using System.Drawing;
using Novolis.Math.Geometry;
using Novolis.Raylib.Rendering;
using Novolis.Rendering.Presentation.Abstractions;

namespace Novolis.Rendering.Presentation.Raylib;

/// <summary>Uploads CPU RGBA frames to a Raylib texture and draws full-screen.</summary>
public sealed class RaylibCpuFramePresenter : IFramePresenter, IDisposable
{
    private Texture _texture;
    private int _width;
    private int _height;
    private byte[]? _uploadBuffer;

    public void PresentCpuFrame(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        EnsureTexture(width, height);
        CopyToBuffer(pixels);
        Textures.UpdateRgba(_texture, _uploadBuffer!);
        Textures.Draw(_texture, 0, 0, Color.White);
    }

    public void Dispose()
    {
        if (_texture.IsValid)
        {
            Textures.Unload(_texture);
        }
    }

    private void EnsureTexture(int width, int height)
    {
        if (_texture.IsValid && _width == width && _height == height)
        {
            return;
        }

        if (_texture.IsValid)
        {
            Textures.Unload(_texture);
        }

        _width = width;
        _height = height;
        _uploadBuffer = new byte[width * height * 4];
        _texture = Textures.LoadFromRgba(_uploadBuffer, width, height);
    }

    private void CopyToBuffer(ReadOnlySpan<Rgba32> pixels)
    {
        if (_uploadBuffer is null || _uploadBuffer.Length < pixels.Length * 4)
        {
            _uploadBuffer = new byte[pixels.Length * 4];
        }

        var width = _width;
        var height = _height;
        for (var y = 0; y < height; y++)
        {
            var srcRow = y * width;
            var dstRow = (height - 1 - y) * width;
            for (var x = 0; x < width; x++)
            {
                var p = pixels[srcRow + x];
                var i = (dstRow + x) * 4;
                _uploadBuffer[i] = p.R;
                _uploadBuffer[i + 1] = p.G;
                _uploadBuffer[i + 2] = p.B;
                _uploadBuffer[i + 3] = p.A;
            }
        }
    }
}
