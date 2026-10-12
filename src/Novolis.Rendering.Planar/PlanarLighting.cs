using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Host-neutral bounded light field used to tint planar drawables.</summary>
public sealed class PlanarLighting
{
    /// <summary>Ambient RGB contribution before local sources are accumulated.</summary>
    public Rgba32 AmbientColor { get; set; } = Rgba32.White;

    /// <summary>Ambient contribution multiplier.</summary>
    public float AmbientIntensity { get; set; } = 1f;

    /// <summary>Active point, spot, and emissive light sources.</summary>
    public List<PlanarLightSource> Sources { get; } = [];

    /// <summary>Removes all local light sources.</summary>
    public void Clear() => Sources.Clear();

    /// <summary>Applies ambient and local light contribution to a base tint.</summary>
    public Rgba32 Shade(Vector3 world, Rgba32 baseColor)
    {
        if (baseColor.A == 0)
        {
            return baseColor;
        }

        var red = AmbientColor.R / 255f * MathF.Max(0f, AmbientIntensity);
        var green = AmbientColor.G / 255f * MathF.Max(0f, AmbientIntensity);
        var blue = AmbientColor.B / 255f * MathF.Max(0f, AmbientIntensity);
        foreach (var source in Sources)
        {
            var radius = MathF.Max(0.01f, source.Radius);
            var delta = world - source.Position;
            var distance = delta.LengthXz();
            if (distance >= radius)
            {
                continue;
            }

            var attenuation = 1f - distance / radius;
            attenuation *= attenuation;
            if (source.Kind == PlanarLightKind.Spot)
            {
                var direction = source.Direction.NormalizeXz();
                var toPoint = delta.NormalizeXz();
                var cutoff = MathF.Cos(System.Math.Clamp(source.ConeAngleRadians, 0.05f, MathF.PI));
                var cone = Vector3.Dot(direction, toPoint);
                if (cone <= cutoff)
                {
                    continue;
                }

                attenuation *= SmoothStep(cutoff, 1f, cone);
            }

            var contribution = attenuation * MathF.Max(0f, source.Intensity);
            red += source.Color.R / 255f * contribution;
            green += source.Color.G / 255f * contribution;
            blue += source.Color.B / 255f * contribution;
        }

        return new Rgba32(
            Scale(baseColor.R, red),
            Scale(baseColor.G, green),
            Scale(baseColor.B, blue),
            baseColor.A);
    }

    private static byte Scale(byte channel, float intensity) =>
        (byte)System.Math.Clamp((int)(channel * MathF.Min(1.75f, MathF.Max(0f, intensity))), 0, 255);

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        var t = System.Math.Clamp((value - edge0) / MathF.Max(0.0001f, edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
