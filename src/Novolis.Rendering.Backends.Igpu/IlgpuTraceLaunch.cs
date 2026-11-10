namespace Novolis.Rendering.Backends.Igpu;

/// <summary>Blittable launch constants for <see cref="IlgpuPathTracerKernels.TracePixelKernel"/>.</summary>
public struct IlgpuTraceLaunch
{
    /// <summary>Framebuffer width.</summary>
    public int Width;

    /// <summary>Framebuffer height.</summary>
    public int Height;

    /// <summary>First sample index in this batch.</summary>
    public int StartSampleIndex;

    /// <summary>Samples to integrate in this launch.</summary>
    public int SampleCount;

    /// <summary>Max path depth.</summary>
    public int MaxDepth;
}
