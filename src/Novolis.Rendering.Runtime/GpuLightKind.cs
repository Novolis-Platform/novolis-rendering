using System.Numerics;

namespace Novolis.Rendering.Runtime;

/// <summary>Runtime light classification for GPU kernels.</summary>
public enum GpuLightKind : byte
{
    /// <summary>Directional light with direction stored in <see cref="GpuLight.DirectionOrPosition"/>.</summary>
    Directional,

    /// <summary>Point light with position stored in <see cref="GpuLight.DirectionOrPosition"/>.</summary>
    Point,
}
