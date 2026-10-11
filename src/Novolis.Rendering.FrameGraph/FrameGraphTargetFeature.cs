namespace Novolis.Rendering.FrameGraph;

/// <summary>Features that a target or quality profile may expose.</summary>
[Flags]
public enum FrameGraphTargetFeature
{
    /// <summary>No optional feature.</summary>
    None = 0,
    /// <summary>High-dynamic-range color processing.</summary>
    Hdr = 1 << 0,
    /// <summary>Uppercase alias for <see cref="Hdr"/>.</summary>
    HDR = Hdr,
    /// <summary>Depth testing or depth sampling.</summary>
    Depth = 1 << 1,
    /// <summary>Emission accumulation.</summary>
    Emission = 1 << 2,
    /// <summary>Bloom extraction and composition.</summary>
    Bloom = 1 << 3,
    /// <summary>Tone mapping into output encoding.</summary>
    ToneMapping = 1 << 4,
    /// <summary>Physically based material evaluation.</summary>
    Pbr = 1 << 5,
    /// <summary>Uppercase alias for <see cref="Pbr"/>.</summary>
    PBR = Pbr,
    /// <summary>Shadow map or ray-traced shadow evaluation.</summary>
    Shadows = 1 << 6,
    /// <summary>Sorted or weighted transparent composition.</summary>
    Transparency = 1 << 7,
    /// <summary>CPU or GPU particle layer composition.</summary>
    Particles = 1 << 8,
    /// <summary>Projected or attached decal composition.</summary>
    Decals = 1 << 9,
    /// <summary>Volumetric density and scattering integration.</summary>
    Volumetrics = 1 << 10,
    /// <summary>Color grading after tone mapping.</summary>
    ColorGrading = 1 << 11,
    /// <summary>Vignette post-processing.</summary>
    Vignette = 1 << 12,
    /// <summary>Screen-space distortion post-processing.</summary>
    Distortion = 1 << 13,
    /// <summary>Diagnostic overlay composition.</summary>
    Diagnostics = 1 << 14,
    /// <summary>Host presentation or present-state transition.</summary>
    Presentation = 1 << 15,
    /// <summary>All declared target features.</summary>
    All = Hdr
        | Depth
        | Emission
        | Bloom
        | ToneMapping
        | Pbr
        | Shadows
        | Transparency
        | Particles
        | Decals
        | Volumetrics
        | ColorGrading
        | Vignette
        | Distortion
        | Diagnostics
        | Presentation,
}
