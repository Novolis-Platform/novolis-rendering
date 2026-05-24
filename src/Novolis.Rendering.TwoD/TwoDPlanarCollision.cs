using System.Numerics;
using TopologyPolygon = Novolis.Math.Topology.Polygon;

namespace Novolis.Rendering.TwoD;

/// <summary>XZ planar collision helpers (no <c>Vector2</c>).</summary>
public static class TwoDPlanarCollision
{
    /// <summary>Point-in-polygon test on the XZ plane.</summary>
    /// <param name="polygon">Polygon vertices.</param>
    /// <param name="x">World X.</param>
    /// <param name="z">World Z.</param>
    public static bool ContainsPoint(TopologyPolygon polygon, float x, float z)
    {
        if (polygon.Length < 3)
        {
            return false;
        }

        var inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            var xi = polygon[i].X;
            var zi = polygon[i].Z;
            var xj = polygon[j].X;
            var zj = polygon[j].Z;
            var intersect = zi > z != zj > z && x < (xj - xi) * (z - zi) / (zj - zi) + xi;
            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>Circle overlap against polygon edges and interior (XZ plane).</summary>
    /// <param name="polygon">Collider polygon.</param>
    /// <param name="centerX">Circle center X.</param>
    /// <param name="centerZ">Circle center Z.</param>
    /// <param name="radius">Circle radius in world units.</param>
    public static bool CircleOverlaps(TopologyPolygon polygon, float centerX, float centerZ, float radius)
    {
        if (ContainsPoint(polygon, centerX, centerZ))
        {
            return true;
        }

        var r2 = radius * radius;
        foreach (var edge in polygon.EdgesSpan)
        {
            var ax = edge.A.X;
            var az = edge.A.Z;
            var bx = edge.B.X;
            var bz = edge.B.Z;
            var t = ClosestPointOnSegmentT(centerX, centerZ, ax, az, bx, bz);
            var px = ax + t * (bx - ax);
            var pz = az + t * (bz - az);
            var dx = centerX - px;
            var dz = centerZ - pz;
            if (dx * dx + dz * dz <= r2)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Minimum translation to separate a circle from a solid polygon.</summary>
    /// <param name="polygon">Solid collider.</param>
    /// <param name="centerX">Circle center X.</param>
    /// <param name="centerZ">Circle center Z.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="separation">XZ separation vector (zero when not overlapping).</param>
    /// <returns>Whether the circle overlaps the polygon.</returns>
    public static bool TryGetCircleSeparation(
        TopologyPolygon polygon,
        float centerX,
        float centerZ,
        float radius,
        out Vector3 separation)
    {
        separation = Vector3.Zero;
        if (!CircleOverlaps(polygon, centerX, centerZ, radius))
        {
            return false;
        }

        var bestLen = 0f;
        var best = Vector3.Zero;

        if (!ContainsPoint(polygon, centerX, centerZ))
        {
            foreach (var edge in polygon.EdgesSpan)
            {
                var ax = edge.A.X;
                var az = edge.A.Z;
                var bx = edge.B.X;
                var bz = edge.B.Z;
                var t = ClosestPointOnSegmentT(centerX, centerZ, ax, az, bx, bz);
                var px = ax + t * (bx - ax);
                var pz = az + t * (bz - az);
                var nx = centerX - px;
                var nz = centerZ - pz;
                var len = MathF.Sqrt(nx * nx + nz * nz);
                if (len < 1e-5f)
                {
                    var ex = bx - ax;
                    var ez = bz - az;
                    var el = MathF.Sqrt(ex * ex + ez * ez);
                    if (el > 1e-5f)
                    {
                        nx = -ez / el;
                        nz = ex / el;
                        len = 0f;
                    }
                    else
                    {
                        continue;
                    }
                }

                var push = radius - len;
                if (push > bestLen)
                {
                    bestLen = push;
                    var inv = 1f / (len > 1e-5f ? len : 1f);
                    best = new Vector3(nx * inv * push, 0f, nz * inv * push);
                }
            }
        }
        else
        {
            for (var i = 0; i < polygon.Length; i++)
            {
                var vx = polygon[i].X;
                var vz = polygon[i].Z;
                var dx = centerX - vx;
                var dz = centerZ - vz;
                var len = MathF.Sqrt(dx * dx + dz * dz);
                if (len < 1e-5f)
                {
                    continue;
                }

                var push = radius + len;
                if (push > bestLen)
                {
                    bestLen = push;
                    var inv = 1f / len;
                    best = new Vector3(dx * inv * push, 0f, dz * inv * push);
                }
            }
        }

        separation = best;
        return bestLen > 0f;
    }

    private static float ClosestPointOnSegmentT(float px, float pz, float ax, float az, float bx, float bz)
    {
        var dx = bx - ax;
        var dz = bz - az;
        var len2 = dx * dx + dz * dz;
        if (len2 < 1e-8f)
        {
            return 0f;
        }

        var t = ((px - ax) * dx + (pz - az) * dz) / len2;
        return System.Math.Clamp(t, 0f, 1f);
    }
}
