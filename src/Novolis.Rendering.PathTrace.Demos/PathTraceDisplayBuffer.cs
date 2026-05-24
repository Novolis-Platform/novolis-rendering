using Novolis.Math.Geometry;
using Novolis.Rendering.Presentation.Abstractions;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>Thread-safe CPU frame buffer for presenters (Silk OpenGL or Raylib).</summary>
public sealed class PathTraceDisplayBuffer
{
    private readonly object _gate = new();
    private Rgba32[]? _pixels;
    private int _width;
    private int _height;

    /// <summary>Sample count of the last published frame.</summary>
    public int DisplayedSampleCount { get; private set; }

    /// <summary>Clears pixels and resets the displayed sample count.</summary>
    /// <param name="width">Framebuffer width.</param>
    /// <param name="height">Framebuffer height.</param>
    public void Invalidate(int width, int height)
    {
        lock (_gate)
        {
            var count = width * height;
            if (_pixels is null || _pixels.Length != count)
            {
                _pixels = new Rgba32[count];
            }

            _width = width;
            _height = height;
            DisplayedSampleCount = 0;
            Array.Clear(_pixels);
        }
    }

    /// <summary>Copies a traced frame into the display buffer.</summary>
    /// <param name="source">RGBA pixels.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="sampleCount">Accumulated sample count from the backend.</param>
    public void Publish(ReadOnlySpan<Rgba32> source, int width, int height, int sampleCount)
    {
        lock (_gate)
        {
            var count = width * height;
            if (_pixels is null || _pixels.Length != count)
            {
                _pixels = new Rgba32[count];
            }

            source.CopyTo(_pixels);
            _width = width;
            _height = height;
            DisplayedSampleCount = sampleCount;
        }
    }

    /// <summary>Presents the latest frame when dimensions are valid.</summary>
    /// <param name="presenter">Host presenter.</param>
    /// <returns><see langword="true"/> when a frame was presented.</returns>
    public bool TryPresent(IFramePresenter presenter)
    {
        lock (_gate)
        {
            if (_pixels is null || _width <= 0 || _height <= 0)
            {
                return false;
            }

            presenter.PresentCpuFrame(_pixels, _width, _height);
            return true;
        }
    }
}
