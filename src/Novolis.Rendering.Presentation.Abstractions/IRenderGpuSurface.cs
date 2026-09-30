using Novolis.Math.Geometry;

namespace Novolis.Rendering.Presentation.Abstractions;

/// <summary>Opaque GPU surface produced by a GPU backend; interpreted only by matching presenters.</summary>
public interface IRenderGpuSurface
{
    /// <summary>Native handle for interop (API-specific).</summary>
    nint NativeHandle { get; }

    /// <summary>Surface width in pixels.</summary>
    int Width { get; }

    /// <summary>Surface height in pixels.</summary>
    int Height { get; }
}
