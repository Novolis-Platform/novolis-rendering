using System.Numerics;

namespace Novolis.Rendering.Materials;

/// <summary>
/// Authoring presets for path-trace materials. SilkTraceStudio / ShowcaseScenes are the
/// capability test for this catalog — prefer these factories over ad-hoc <see cref="Vector3"/> literals.
/// </summary>
public static class MaterialPresets
{
    /// <summary>Creates a standard (dielectric) material.</summary>
    public static StandardMaterial Standard(Vector3 color, float roughness = 0.5f, float metallic = 0f) =>
        new() { BaseColor = color, Roughness = roughness, Metallic = metallic };

    /// <summary>Creates a metallic standard material.</summary>
    public static StandardMaterial Metal(Vector3 color, float roughness = 0.08f) =>
        new() { BaseColor = color, Roughness = roughness, Metallic = 1f };

    /// <summary>Creates a glass material.</summary>
    public static GlassMaterial Glass(Vector3 tint, float roughness = 0f, float ior = 1.5f, float absorption = 0f) =>
        new() { Tint = tint, Roughness = roughness, Ior = ior, Absorption = absorption };

    /// <summary>Creates a skin material (subsurface approx in the path tracer).</summary>
    public static SkinMaterial Skin(Vector3 baseColor, float roughness = 0.45f) =>
        new() { BaseColor = baseColor, Roughness = roughness };

    /// <summary>Creates an emissive material (mesh light; also extracted to point-light NEE).</summary>
    public static EmissiveMaterial Emissive(Vector3 color, float strength = 1f) =>
        new() { EmissionColor = color, EmissionStrength = strength };

    /// <summary>High-intensity neon tube / lightsaber blade (emissive).</summary>
    public static EmissiveMaterial Neon(Vector3 color, float strength = 18f) =>
        Emissive(color, strength);

    /// <summary>Common preset colors for demos and tests.</summary>
    public static class Colors
    {
        /// <summary>Neutral silver metal tone.</summary>
        public static Vector3 Silver => new(0.92f, 0.92f, 0.95f);

        /// <summary>White albedo.</summary>
        public static Vector3 White => Vector3.One;

        /// <summary>Red albedo.</summary>
        public static Vector3 Red => new(0.9f, 0.15f, 0.1f);

        /// <summary>Cornell-style green wall.</summary>
        public static Vector3 Green => new(0.08f, 0.55f, 0.14f);

        /// <summary>Warm gold metal.</summary>
        public static Vector3 Gold => new(0.92f, 0.72f, 0.28f);

        /// <summary>Copper metal.</summary>
        public static Vector3 Copper => new(0.85f, 0.42f, 0.22f);

        /// <summary>Cyan neon.</summary>
        public static Vector3 CyanNeon => new(0.35f, 0.95f, 1f);

        /// <summary>Magenta neon.</summary>
        public static Vector3 MagentaNeon => new(1f, 0.2f, 0.75f);

        /// <summary>Warm softbox white.</summary>
        public static Vector3 Softbox => new(1f, 0.96f, 0.9f);

        /// <summary>Flesh / skin tone.</summary>
        public static Vector3 Flesh => new(0.78f, 0.52f, 0.42f);
    }

    /// <summary>
    /// Named looks for the path-trace capability diorama. Every
    /// <see cref="Runtime.MaterialModel"/> is represented so SilkTraceStudio dogfoods Materials end-to-end.
    /// </summary>
    public static class Showcase
    {
        /// <summary>Matte Cornell red wall.</summary>
        public static StandardMaterial CornellRedWall() =>
            Standard(new Vector3(0.78f, 0.08f, 0.06f), roughness: 0.72f);

        /// <summary>Matte Cornell green wall.</summary>
        public static StandardMaterial CornellGreenWall() =>
            Standard(Colors.Green, roughness: 0.72f);

        /// <summary>Neutral back wall / ceiling plaster.</summary>
        public static StandardMaterial Plaster() =>
            Standard(new Vector3(0.88f, 0.88f, 0.86f), roughness: 0.82f);

        /// <summary>Light checker tile.</summary>
        public static StandardMaterial CheckerLight() =>
            Standard(new Vector3(0.82f, 0.8f, 0.74f), roughness: 0.9f);

        /// <summary>Dark checker tile.</summary>
        public static StandardMaterial CheckerDark() =>
            Standard(new Vector3(0.16f, 0.16f, 0.18f), roughness: 0.88f);

        /// <summary>Matte red die.</summary>
        public static StandardMaterial MatteRed() =>
            Standard(Colors.Red, roughness: 0.55f, metallic: 0.02f);

        /// <summary>Rough blue plastic.</summary>
        public static StandardMaterial BluePlastic() =>
            Standard(new Vector3(0.15f, 0.28f, 0.78f), roughness: 0.7f);

        /// <summary>Glossy white ceramic.</summary>
        public static StandardMaterial Ceramic() =>
            Standard(new Vector3(0.95f, 0.95f, 0.93f), roughness: 0.12f);

        /// <summary>Dark rubber.</summary>
        public static StandardMaterial Rubber() =>
            Standard(new Vector3(0.05f, 0.05f, 0.06f), roughness: 0.95f);

        /// <summary>Brushed gold column.</summary>
        public static StandardMaterial BrushedGold() =>
            Metal(Colors.Gold, roughness: 0.18f);

        /// <summary>Mirror chrome slab.</summary>
        public static StandardMaterial Mirror() =>
            Metal(Colors.Silver, roughness: 0.03f);

        /// <summary>Copper block.</summary>
        public static StandardMaterial Copper() =>
            Metal(Colors.Copper, roughness: 0.22f);

        /// <summary>Lightsaber hilt metal.</summary>
        public static StandardMaterial SaberHilt() =>
            Metal(new Vector3(0.12f, 0.12f, 0.14f), roughness: 0.35f);

        /// <summary>Clear glass cube.</summary>
        public static GlassMaterial ClearGlass() =>
            Glass(tint: new Vector3(0.92f, 0.96f, 1f), roughness: 0.01f, ior: 1.5f, absorption: 0.15f);

        /// <summary>Green tinted glass.</summary>
        public static GlassMaterial GreenGlass() =>
            Glass(tint: new Vector3(0.45f, 0.85f, 0.55f), roughness: 0.04f, ior: 1.45f, absorption: 0.55f);

        /// <summary>Flesh / skin SSS stand-in.</summary>
        public static SkinMaterial Flesh() =>
            Skin(Colors.Flesh, roughness: 0.48f);

        /// <summary>Ceiling softbox panel.</summary>
        public static EmissiveMaterial Softbox() =>
            Emissive(Colors.Softbox, strength: 8f);

        /// <summary>Cyan lightsaber blade.</summary>
        public static EmissiveMaterial LightsaberCyan() =>
            Neon(Colors.CyanNeon, strength: 14f);

        /// <summary>Magenta neon stick.</summary>
        public static EmissiveMaterial NeonMagenta() =>
            Neon(Colors.MagentaNeon, strength: 10f);

        /// <summary>Warm accent bulb.</summary>
        public static EmissiveMaterial WarmAccent() =>
            Emissive(new Vector3(1f, 0.55f, 0.2f), strength: 4f);
    }
}
