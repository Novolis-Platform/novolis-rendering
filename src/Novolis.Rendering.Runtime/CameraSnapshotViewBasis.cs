using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Runtime;

/// <summary>Bridges <see cref="CameraSnapshot"/> to <see cref="ViewBasis"/> for shared ray math.</summary>
public static class CameraSnapshotViewBasis
{
    /// <summary>Orthonormal view axes for this snapshot.</summary>
    public static ViewBasis ToViewBasis(in CameraSnapshot snapshot) =>
        new(snapshot.Forward, snapshot.Right, snapshot.Up);

    /// <summary>Primary ray direction for normalized device coordinates.</summary>
    public static Vector3 PrimaryRayDirection(in CameraSnapshot snapshot, float u, float v)
    {
        var tanHalf = MathF.Tan(snapshot.VerticalFovRadians * 0.5f);
        return ViewBasis.PrimaryRayDirection(ToViewBasis(snapshot), u, v, tanHalf, snapshot.AspectRatio);
    }
}
