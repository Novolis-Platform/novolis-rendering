using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>Authoring presets for common material looks.</summary>
public static class MaterialPresets
{
    /// <summary>Creates a standard (dielectric) material.</summary>
    /// <param name="color">Base color in linear RGB.</param>
    /// <param name="roughness">Surface roughness.</param>
    /// <param name="metallic">Metallic factor.</param>
    /// <returns>A configured <see cref="StandardMaterial"/>.</returns>
    public static StandardMaterial Standard(Vector3 color, float roughness = 0.5f, float metallic = 0f) =>
        new() { BaseColor = color, Roughness = roughness, Metallic = metallic };

    /// <summary>Creates a metallic standard material.</summary>
    /// <param name="color">Base color in linear RGB.</param>
    /// <param name="roughness">Surface roughness.</param>
    /// <returns>A metallic <see cref="StandardMaterial"/>.</returns>
    public static StandardMaterial Metal(Vector3 color, float roughness = 0.08f) =>
        new() { BaseColor = color, Roughness = roughness, Metallic = 1f };

    /// <summary>Creates a glass material.</summary>
    /// <param name="tint">Transmission tint.</param>
    /// <param name="roughness">Surface roughness.</param>
    /// <param name="ior">Index of refraction.</param>
    /// <returns>A configured <see cref="GlassMaterial"/>.</returns>
    public static GlassMaterial Glass(Vector3 tint, float roughness = 0f, float ior = 1.5f) =>
        new() { Tint = tint, Roughness = roughness, Ior = ior };

    /// <summary>Creates a skin material.</summary>
    /// <param name="baseColor">Skin tone.</param>
    /// <param name="roughness">Surface roughness.</param>
    /// <returns>A configured <see cref="SkinMaterial"/>.</returns>
    public static SkinMaterial Skin(Vector3 baseColor, float roughness = 0.45f) =>
        new() { BaseColor = baseColor, Roughness = roughness };

    /// <summary>Creates an emissive material.</summary>
    /// <param name="color">Emission color.</param>
    /// <param name="strength">Emission strength.</param>
    /// <returns>A configured <see cref="EmissiveMaterial"/>.</returns>
    public static EmissiveMaterial Emissive(Vector3 color, float strength = 1f) =>
        new() { EmissionColor = color, EmissionStrength = strength };

    /// <summary>Common preset colors for demos and tests.</summary>
    public static class Colors
    {
        /// <summary>Neutral silver metal tone.</summary>
        public static Vector3 Silver => new(0.92f, 0.92f, 0.95f);

        /// <summary>White albedo.</summary>
        public static Vector3 White => Vector3.One;

        /// <summary>Red albedo.</summary>
        public static Vector3 Red => new(0.9f, 0.15f, 0.1f);
    }
}
