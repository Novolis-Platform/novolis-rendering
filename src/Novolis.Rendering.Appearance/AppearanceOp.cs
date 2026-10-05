namespace Novolis.Rendering.Appearance;

/// <summary>One composable operation. Meaning depends on the stack <see cref="AppearanceDomain"/>.</summary>
public enum AppearanceOp
{
    /// <summary>Identity grey surface / empty volume / unit light / passthrough post.</summary>
    Identity = 0,

    /// <summary>Surface: replace or mix base color.</summary>
    BaseColor = 1,

    /// <summary>Surface: set roughness in Amount [0,1].</summary>
    Roughness = 2,

    /// <summary>Surface: set metallic in Amount [0,1].</summary>
    Metallic = 3,

    /// <summary>Surface/effect: add emissive Color * Amount; Scale is optional noise mask frequency.</summary>
    Emission = 4,

    /// <summary>Surface/volume: value-noise modulation. Scale = frequency, Amount = amplitude, Seed = field.</summary>
    Noise = 5,

    /// <summary>Surface: dark panel lines. Scale = period in UV, Aux = line width fraction.</summary>
    PanelGrid = 6,

    /// <summary>Surface: brighten high-frequency edges. Amount = strength.</summary>
    EdgeWear = 7,

    /// <summary>Surface: multiply albedo by Color.</summary>
    Multiply = 8,

    /// <summary>Surface: lerp albedo toward Color by Amount.</summary>
    Mix = 9,

    /// <summary>Surface: view-dependent rim. Amount = power.</summary>
    Fresnel = 10,

    /// <summary>Surface: multiply alpha by Amount.</summary>
    Transparency = 11,

    /// <summary>Volume: set density to Amount.</summary>
    Density = 12,

    /// <summary>Volume: absorption tint.</summary>
    Absorption = 13,

    /// <summary>Volume: scattering tint and Amount.</summary>
    Scattering = 14,

    /// <summary>Volume: multiply density by noise.</summary>
    DensityNoise = 15,

    /// <summary>Light: omnidirectional. Amount = intensity.</summary>
    Point = 16,

    /// <summary>Light: cone. Aux = cosine of half-angle.</summary>
    Cone = 17,

    /// <summary>Light: multiply intensity by Amount.</summary>
    Intensity = 18,

    /// <summary>Light/effect: multiply color.</summary>
    Tint = 19,

    /// <summary>Light: time flicker. Scale = Hertz.</summary>
    Flicker = 20,

    /// <summary>Light: distance falloff coefficient in Amount.</summary>
    Falloff = 21,

    /// <summary>Effect: capsule/line beam with radial falloff in UV.</summary>
    Beam = 22,

    /// <summary>Effect: glowing core (UV distance from center).</summary>
    Projectile = 23,

    /// <summary>Effect: sparse spark speckles.</summary>
    SparkBurst = 24,

    /// <summary>Post: threshold bloom. Amount = mix, Aux = threshold, Scale = radius px.</summary>
    Bloom = 25,

    /// <summary>Post: exposure multiplier.</summary>
    Exposure = 26,

    /// <summary>Post: darken edges. Amount = strength.</summary>
    Vignette = 27,

    /// <summary>Post: Reinhard tonemap mix in Amount.</summary>
    ToneMap = 28,
}
