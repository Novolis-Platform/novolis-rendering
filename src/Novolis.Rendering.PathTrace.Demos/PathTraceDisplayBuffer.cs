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

    /// <summary>Increments on each <see cref="Publish"/>.</summary>
    public int FrameGeneration { get; private set; }

    /// <summary>Sample count of the last published frame.</summary>
    public int DisplayedSampleCount { get; private set; }

    /// <summary>Clears pixels and resets the displayed sample count.</summary>
    public void Invalidate(int width, int height)
    {
        lock (_gate)
        {
            var count = width * height;
            if (_pixels is null || _pixels.Length != count)
                _pixels = new Rgba32[count];

            _width = width;
            _height = height;
            DisplayedSampleCount = 0;
            Array.Clear(_pixels);
        }
    }

    /// <summary>Copies a traced frame into the display buffer.</summary>
    public void Publish(ReadOnlySpan<Rgba32> source, int width, int height, int sampleCount)
    {
        lock (_gate)
        {
            var count = width * height;
            if (_pixels is null || _pixels.Length != count)
                _pixels = new Rgba32[count];

            source.CopyTo(_pixels);
            _width = width;
            _height = height;
            DisplayedSampleCount = sampleCount;
            FrameGeneration++;
        }
    }

    /// <summary>Presents the latest frame when dimensions are valid and generation changed.</summary>
    /// <param name="presenter">Host presenter.</param>
    /// <param name="lastPresentedGeneration">Updated when a frame is presented.</param>
    public bool TryPresent(IFramePresenter presenter, ref int lastPresentedGeneration)
    {
        lock (_gate)
        {
            if (_pixels is null || _width <= 0 || _height <= 0)
                return false;

            if (FrameGeneration == lastPresentedGeneration)
                return false;

            presenter.PresentCpuFrame(_pixels, _width, _height);
            lastPresentedGeneration = FrameGeneration;
            return true;
        }
    }

    /// <summary>Presents the latest frame (always uploads).</summary>
    public bool TryPresent(IFramePresenter presenter)
    {
        var generation = -1;
        return TryPresent(presenter, ref generation);
    }
}
