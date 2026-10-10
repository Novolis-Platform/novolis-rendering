using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Axis-aligned world XZ bounds.</summary>
/// <param name="MinX">Left edge.</param>
/// <param name="MinZ">Bottom edge.</param>
/// <param name="MaxX">Right edge.</param>
/// <param name="MaxZ">Top edge.</param>
public readonly record struct PlanarWorldBounds(float MinX, float MinZ, float MaxX, float MaxZ);
