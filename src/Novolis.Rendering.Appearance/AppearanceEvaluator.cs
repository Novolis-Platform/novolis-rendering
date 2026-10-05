using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Appearance;

/// <summary>CPU evaluator. Compilers to GLSL / GpuMaterial can consume the same stacks later.</summary>
public static class AppearanceEvaluator
{
    /// <summary>Evaluates a surface stack at <paramref name="context"/>.</summary>
    public static SurfaceSample EvaluateSurface(AppearanceStack stack, in AppearanceContext context)
    {
        ArgumentNullException.ThrowIfNull(stack);
        var albedo = AppearanceColor.Grey;
        var roughness = 0.5f;
        var metallic = 0f;
        var emission = Rgba32.Black;
        var alpha = 1f;
        foreach (var layer in stack.Layers)
        {
            ApplySurface(layer, context, ref albedo, ref roughness, ref metallic, ref emission, ref alpha);
        }

        return new SurfaceSample(albedo, System.Math.Clamp(roughness, 0f, 1f), System.Math.Clamp(metallic, 0f, 1f), emission, System.Math.Clamp(alpha, 0f, 1f));
    }

    /// <summary>Evaluates a volume stack.</summary>
    public static VolumeSample EvaluateVolume(AppearanceStack stack, in AppearanceContext context)
    {
        ArgumentNullException.ThrowIfNull(stack);
        var density = 0f;
        var scatter = new Rgba32(220, 220, 230);
        var absorb = new Rgba32(40, 40, 48);
        foreach (var layer in stack.Layers)
        {
            switch (layer.Op)
            {
                case AppearanceOp.Density:
                    density = MathF.Max(0f, layer.Amount);
                    break;
                case AppearanceOp.Absorption:
                    absorb = layer.Color.A == 0 && layer.Color.R == 0 ? absorb : layer.Color;
                    break;
                case AppearanceOp.Scattering:
                    scatter = layer.Color.A == 0 && layer.Color.R == 0 ? scatter : layer.Color;
                    density = MathF.Max(density, layer.Amount);
                    break;
                case AppearanceOp.DensityNoise:
                case AppearanceOp.Noise:
                    density *= 0.35f + AppearanceNoise.Value(context.U * layer.Scale, context.V * layer.Scale, layer.Seed + context.Seed) * 1.3f * layer.Amount;
                    break;
            }
        }

        return new VolumeSample(MathF.Max(0f, density), scatter, absorb);
    }

    /// <summary>Evaluates a light stack at a sample relative to the light.</summary>
    public static LightSample EvaluateLight(AppearanceStack stack, in AppearanceContext context)
    {
        ArgumentNullException.ThrowIfNull(stack);
        var color = Rgba32.White;
        var intensity = 1f;
        var falloff = 0.08f;
        var coneCos = -1f;
        foreach (var layer in stack.Layers)
        {
            switch (layer.Op)
            {
                case AppearanceOp.Point:
                    intensity *= MathF.Max(0f, layer.Amount);
                    break;
                case AppearanceOp.Cone:
                    intensity *= MathF.Max(0f, layer.Amount);
                    coneCos = layer.Aux;
                    break;
                case AppearanceOp.Intensity:
                    intensity *= MathF.Max(0f, layer.Amount);
                    break;
                case AppearanceOp.Tint:
                    color = AppearanceColor.Multiply(color, layer.Color.A == 0 ? Rgba32.White : layer.Color);
                    break;
                case AppearanceOp.Flicker:
                    intensity *= 0.82f + 0.18f * (0.5f + 0.5f * MathF.Sin(context.Time * layer.Scale * MathF.PI * 2f + layer.Seed));
                    break;
                case AppearanceOp.Falloff:
                    falloff = MathF.Max(0f, layer.Amount);
                    break;
            }
        }

        var dist = MathF.Max(0f, context.Distance);
        intensity /= 1f + dist * dist * falloff;
        if (coneCos > -0.999f)
        {
            var t = System.Math.Clamp((context.AxisCosine - coneCos) / MathF.Max(1e-4f, 1f - coneCos), 0f, 1f);
            intensity *= t * t;
        }

        return new LightSample(color, MathF.Max(0f, intensity));
    }

    /// <summary>2D preview color: albedo plus emission (hosts without a lighting pass).</summary>
    public static Rgba32 PreviewColor(in SurfaceSample sample)
    {
        var lit = AppearanceColor.Scale(sample.Albedo, 0.72f + sample.Metallic * 0.28f);
        var glow = AppearanceColor.Scale(sample.Emission, 1f);
        return AppearanceColor.WithAlpha(AppearanceColor.Add(lit, glow), sample.Alpha);
    }

    /// <summary>Effect color at UV (beam / projectile / sparks).</summary>
    public static Rgba32 EvaluateEffect(AppearanceStack stack, in AppearanceContext context)
    {
        ArgumentNullException.ThrowIfNull(stack);
        var color = new Rgba32(255, 180, 80, 0);
        foreach (var layer in stack.Layers)
        {
            switch (layer.Op)
            {
                case AppearanceOp.Tint:
                    color = new Rgba32(layer.Color.R, layer.Color.G, layer.Color.B, color.A);
                    break;
                case AppearanceOp.Beam:
                    color = Radial(layer.Color, MathF.Abs(context.U - 0.5f) * 2f, layer.Amount, color.A);
                    break;
                case AppearanceOp.Projectile:
                    var dx = context.U - 0.5f;
                    var dy = context.V - 0.5f;
                    color = Radial(layer.Color, MathF.Sqrt(dx * dx + dy * dy) * 2f, layer.Amount, color.A);
                    break;
                case AppearanceOp.SparkBurst:
                    var speckle = AppearanceNoise.Value(context.U * layer.Scale, context.V * layer.Scale, layer.Seed);
                    if (speckle > 1f - layer.Amount * 0.12f)
                    {
                        color = new Rgba32(layer.Color.R, layer.Color.G, layer.Color.B, 255);
                    }

                    break;
                case AppearanceOp.Emission:
                    var add = AppearanceColor.Scale(layer.Color, layer.Amount);
                    color = new Rgba32(
                        (byte)System.Math.Clamp(color.R + add.R, 0, 255),
                        (byte)System.Math.Clamp(color.G + add.G, 0, 255),
                        (byte)System.Math.Clamp(color.B + add.B, 0, 255),
                        color.A);
                    break;
            }
        }

        return color;
    }

    private static void ApplySurface(
        in AppearanceLayer layer,
        in AppearanceContext context,
        ref Rgba32 albedo,
        ref float roughness,
        ref float metallic,
        ref Rgba32 emission,
        ref float alpha)
    {
        var n = AppearanceNoise.Value(context.U * (layer.Scale <= 0f ? 1f : layer.Scale), context.V * (layer.Scale <= 0f ? 1f : layer.Scale), layer.Seed + context.Seed);
        switch (layer.Op)
        {
            case AppearanceOp.BaseColor:
                albedo = AppearanceColor.Lerp(albedo, layer.Color, System.Math.Clamp(layer.Amount, 0f, 1f));
                break;
            case AppearanceOp.Roughness:
                roughness = layer.Amount;
                break;
            case AppearanceOp.Metallic:
                metallic = layer.Amount;
                break;
            case AppearanceOp.Emission:
                var mask = layer.Scale > 0f ? n : 1f;
                emission = AppearanceColor.Add(emission, AppearanceColor.Scale(layer.Color, layer.Amount * mask));
                break;
            case AppearanceOp.Noise:
                var delta = (n - 0.5f) * layer.Amount;
                albedo = AppearanceColor.Scale(albedo, 1f + delta);
                roughness = System.Math.Clamp(roughness + delta * 0.35f, 0f, 1f);
                break;
            case AppearanceOp.PanelGrid:
                var fu = Frac(context.U / MathF.Max(1e-4f, layer.Scale));
                var fv = Frac(context.V / MathF.Max(1e-4f, layer.Scale));
                var width = layer.Aux > 0f ? layer.Aux : 0.06f;
                var line = fu < width || fv < width || fu > 1f - width || fv > 1f - width;
                if (line)
                {
                    albedo = AppearanceColor.Scale(albedo, 1f - System.Math.Clamp(layer.Amount, 0f, 1f) * 0.65f);
                }

                break;
            case AppearanceOp.EdgeWear:
                var wear = n * layer.Amount;
                albedo = AppearanceColor.Lerp(albedo, new Rgba32(210, 205, 190), wear * 0.55f);
                roughness = System.Math.Clamp(roughness + wear * 0.2f, 0f, 1f);
                break;
            case AppearanceOp.Multiply:
                albedo = AppearanceColor.Multiply(albedo, layer.Color.A == 0 ? Rgba32.White : layer.Color);
                break;
            case AppearanceOp.Mix:
                albedo = AppearanceColor.Lerp(albedo, layer.Color, System.Math.Clamp(layer.Amount, 0f, 1f));
                break;
            case AppearanceOp.Fresnel:
                var ndv = System.Math.Clamp(Vector3.Dot(Safe(context.Normal), Safe(context.View)), 0f, 1f);
                var rim = MathF.Pow(1f - ndv, MathF.Max(0.1f, layer.Amount));
                emission = AppearanceColor.Add(emission, AppearanceColor.Scale(layer.Color.A == 0 ? Rgba32.White : layer.Color, rim));
                break;
            case AppearanceOp.Transparency:
                alpha *= System.Math.Clamp(layer.Amount, 0f, 1f);
                break;
        }
    }

    private static Rgba32 Radial(Rgba32 color, float dist, float amount, byte previousAlpha)
    {
        var fall = System.Math.Clamp(1f - dist, 0f, 1f);
        fall = MathF.Pow(fall, MathF.Max(0.2f, amount));
        var a = (byte)System.Math.Clamp((int)(fall * 255f), 0, 255);
        if (a < previousAlpha)
        {
            a = previousAlpha;
        }

        return new Rgba32(
            (byte)System.Math.Clamp((int)(color.R * fall), 0, 255),
            (byte)System.Math.Clamp((int)(color.G * fall), 0, 255),
            (byte)System.Math.Clamp((int)(color.B * fall), 0, 255),
            a);
    }

    private static float Frac(float v) => v - MathF.Floor(v);

    private static Vector3 Safe(Vector3 v) => v.LengthSquared() < 1e-8f ? Vector3.UnitY : Vector3.Normalize(v);
}
