using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Materials;

/// <summary>Authoring presets for common material looks.</summary>
public static class Materials
{
    public static StandardMaterial Standard(Vector3 color, float roughness = 0.5f, float metallic = 0f) =>
        new() { BaseColor = color, Roughness = roughness, Metallic = metallic };

    public static StandardMaterial Metal(Vector3 color, float roughness = 0.08f) =>
        new() { BaseColor = color, Roughness = roughness, Metallic = 1f };

    public static GlassMaterial Glass(Vector3 tint, float roughness = 0f, float ior = 1.5f) =>
        new() { Tint = tint, Roughness = roughness, Ior = ior };

    public static SkinMaterial Skin(Vector3 baseColor, float roughness = 0.45f) =>
        new() { BaseColor = baseColor, Roughness = roughness };

    public static EmissiveMaterial Emissive(Vector3 color, float strength = 1f) =>
        new() { EmissionColor = color, EmissionStrength = strength };

    public static class Colors
    {
        public static Vector3 Silver => new(0.92f, 0.92f, 0.95f);
        public static Vector3 White => Vector3.One;
        public static Vector3 Red => new(0.9f, 0.15f, 0.1f);
    }
}
