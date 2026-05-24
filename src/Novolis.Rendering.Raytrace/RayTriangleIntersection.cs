using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Raytrace;

internal static class RayTriangleIntersection
{
    private const float Epsilon = 1e-7f;

    public static bool TryHit(in Ray3 ray, Vector3 v0, Vector3 v1, Vector3 v2, float maxDistance, out float distance, out Vector3 normal)
    {
        distance = 0f;
        normal = default;

        var edge1 = v1 - v0;
        var edge2 = v2 - v0;
        var pvec = Vector3.Cross(ray.Direction, edge2);
        var det = Vector3.Dot(edge1, pvec);
        if (MathF.Abs(det) < Epsilon)
        {
            return false;
        }

        var invDet = 1f / det;
        var tvec = ray.Origin - v0;
        var u = Vector3.Dot(tvec, pvec) * invDet;
        if (u is < 0f or > 1f)
        {
            return false;
        }

        var qvec = Vector3.Cross(tvec, edge1);
        var v = Vector3.Dot(ray.Direction, qvec) * invDet;
        if (v < 0f || u + v > 1f)
        {
            return false;
        }

        var t = Vector3.Dot(edge2, qvec) * invDet;
        if (t < Epsilon || t > maxDistance)
        {
            return false;
        }

        distance = t;
        normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
        if (Vector3.Dot(normal, ray.Direction) > 0f)
        {
            normal = -normal;
        }

        return true;
    }
}
