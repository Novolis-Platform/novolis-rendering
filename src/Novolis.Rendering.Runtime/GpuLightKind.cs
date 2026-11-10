namespace Novolis.Rendering.Runtime;

/// <summary>Runtime light classification for GPU kernels.</summary>
public enum GpuLightKind : byte
{
    /// <summary>Directional light with direction stored in <see cref="GpuLight.DirectionOrPosition"/>.</summary>
    Directional,

    /// <summary>Point light with position stored in <see cref="GpuLight.DirectionOrPosition"/>.</summary>
    Point,

    /// <summary>
    /// Rectangular area light: center in <see cref="GpuLight.DirectionOrPosition"/>,
    /// half-extent axes in <see cref="GpuLight.U"/> / <see cref="GpuLight.V"/>.
    /// </summary>
    Rect,
}
