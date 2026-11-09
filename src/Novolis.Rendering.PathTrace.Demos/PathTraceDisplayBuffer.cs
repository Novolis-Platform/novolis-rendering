using Novolis.Math.Geometry;
using Novolis.Rendering.Presentation.Abstractions;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>
/// Triple-buffered CPU frame ring for presenters (Silk OpenGL or Raylib).
/// Publish writes a free slot; present returns that slot without an extra memcpy.
/// </summary>
public sealed class PathTraceDisplayBuffer
{
    private readonly object _gate = new();
    private readonly Rgba32[]?[] _slots = new Rgba32[3][];
    private int _write;
    private int _ready;
    private int _presented;
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
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] is null || _slots[i]!.Length != count)
                {
                    _slots[i] = new Rgba32[count];
                }

                Array.Clear(_slots[i]!);
            }

            _width = width;
            _height = height;
            _write = 0;
            _ready = 0;
            _presented = 0;
            DisplayedSampleCount = 0;
            FrameGeneration++;
        }
    }

    /// <summary>Copies a traced frame into the next free ring slot and publishes it.</summary>
    public void Publish(ReadOnlySpan<Rgba32> source, int width, int height, int sampleCount)
    {
        lock (_gate)
        {
            var count = width * height;
            EnsureSlotsLocked(count);

            var slot = FindWritableSlotLocked();
            source.CopyTo(_slots[slot]);
            _ready = slot;
            _write = (slot + 1) % _slots.Length;
            _width = width;
            _height = height;
            DisplayedSampleCount = sampleCount;
            FrameGeneration++;
        }
    }

    /// <summary>Presents the latest frame when dimensions are valid and generation changed.</summary>
    public bool TryPresent(IFramePresenter presenter, ref int lastPresentedGeneration)
    {
        lock (_gate)
        {
            if (_slots[_ready] is null || _width <= 0 || _height <= 0)
            {
                return false;
            }

            if (FrameGeneration == lastPresentedGeneration)
            {
                return false;
            }

            presenter.PresentCpuFrame(_slots[_ready]!, _width, _height);
            _presented = _ready;
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

    /// <summary>Returns the latest CPU frame for a host blit (Silk, etc.).</summary>
    public bool TryCopyFrame(out Rgba32[] pixels, out int width, out int height)
    {
        lock (_gate)
        {
            if (_slots[_ready] is null || _width <= 0 || _height <= 0)
            {
                pixels = [];
                width = 0;
                height = 0;
                return false;
            }

            pixels = _slots[_ready]!;
            _presented = _ready;
            width = _width;
            height = _height;
            return true;
        }
    }

    /// <summary>
    /// Like <see cref="TryCopyFrame"/> but only succeeds when <see cref="FrameGeneration"/> advanced.
    /// Returns the ring slot directly (no present memcpy).
    /// </summary>
    public bool TryCopyFrameIfNew(ref int lastPresentedGeneration, out Rgba32[] pixels, out int width, out int height)
    {
        lock (_gate)
        {
            if (_slots[_ready] is null || _width <= 0 || _height <= 0 || FrameGeneration == lastPresentedGeneration)
            {
                pixels = [];
                width = 0;
                height = 0;
                return false;
            }

            pixels = _slots[_ready]!;
            _presented = _ready;
            width = _width;
            height = _height;
            lastPresentedGeneration = FrameGeneration;
            return true;
        }
    }

    private int FindWritableSlotLocked()
    {
        for (var i = 0; i < _slots.Length; i++)
        {
            var slot = (_write + i) % _slots.Length;
            if (slot != _presented)
            {
                return slot;
            }
        }

        return (_presented + 1) % _slots.Length;
    }

    private void EnsureSlotsLocked(int count)
    {
        for (var i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] is null || _slots[i]!.Length != count)
            {
                _slots[i] = new Rgba32[count];
            }
        }
    }
}
