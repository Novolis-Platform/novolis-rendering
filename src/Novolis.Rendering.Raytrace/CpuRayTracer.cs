using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;

namespace Novolis.Rendering.Raytrace;

/// <summary>Brute-force CPU ray tracer (BVH acceleration can be added without API changes).</summary>
public sealed class CpuRayTracer : IRayTracer
{
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.4f, -1f, -0.3f));
    private static readonly Rgba32 SkyLow = Rgba32.FromArgb(255, 40, 44, 52);
    private static readonly Rgba32 SkyHigh = Rgba32.FromArgb(255, 120, 168, 220);

    public void Render(ImageBuffer target, in RenderCamera camera, RenderScene scene)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(scene);

        var width = target.Width;
        var height = target.Height;
        var aspect = (float)width / height;
        var tanHalfFov = MathF.Tan(camera.VerticalFovRadians * 0.5f);

        for (var y = 0; y < height; y++)
        {
            var v = (2f * (y + 0.5f) / height - 1f) * tanHalfFov;
            for (var x = 0; x < width; x++)
            {
                var u = (2f * (x + 0.5f) / width - 1f) * tanHalfFov * aspect;
                var direction = Vector3.Normalize(camera.Forward + u * camera.Right + v * camera.Up);
                var ray = new Ray3(camera.Position, direction);
                target[x, y] = Trace(ray, scene);
            }
        }
    }

    private static Rgba32 Trace(in Ray3 ray, RenderScene scene)
    {
        var hit = false;
        var bestT = float.MaxValue;
        var bestNormal = Vector3.UnitY;
        var bestColor = Rgba32.White;

        foreach (var mesh in scene.Meshes)
        {
            for (var tri = 0; tri < mesh.TriangleCount; tri++)
            {
                mesh.GetTriangle(tri, out var v0, out var v1, out var v2);
                if (!RayTriangleIntersection.TryHit(in ray, v0, v1, v2, bestT, out var t, out var normal))
                {
                    continue;
                }

                hit = true;
                bestT = t;
                bestNormal = normal;
                bestColor = mesh.Color;
            }
        }

        if (!hit)
        {
            return SampleSky(ray.Direction);
        }

        var ndotl = MathF.Max(0.1f, Vector3.Dot(bestNormal, -LightDirection));
        return Scale(bestColor, ndotl);
    }

    private static Rgba32 SampleSky(Vector3 direction)
    {
        var t = System.Math.Clamp(direction.Y * 0.5f + 0.5f, 0f, 1f);
        return Lerp(SkyLow, SkyHigh, t);
    }

    private static Rgba32 Scale(Rgba32 color, float factor) =>
        new(
            (byte)System.Math.Clamp((int)(color.R * factor), 0, 255),
            (byte)System.Math.Clamp((int)(color.G * factor), 0, 255),
            (byte)System.Math.Clamp((int)(color.B * factor), 0, 255),
            color.A);

    private static Rgba32 Lerp(Rgba32 a, Rgba32 b, float t) =>
        new(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            255);
}
