using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Deterministic light description consumed by planar tessellation.</summary>
public readonly record struct PlanarLightSource(
    Vector3 Position,
    Rgba32 Color,
    float Radius,
    float Intensity,
    PlanarLightKind Kind,
    Vector3 Direction,
    float ConeAngleRadians);
