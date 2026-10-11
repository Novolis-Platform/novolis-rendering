namespace Novolis.Rendering.FrameGraph;

/// <summary>Bit mask of pipeline stages supported by a target or quality profile.</summary>
[Flags]
public enum FrameGraphPipelineStageMask
{
    /// <summary>No stages.</summary>
    None = 0,

    /// <summary>Camera, view, scene, and visibility preparation.</summary>
    Preparation = 1 << 0,

    /// <summary>Depth and visibility generation for opaque geometry.</summary>
    DepthVisibility = 1 << 1,

    /// <summary>Opaque geometry submission before lighting.</summary>
    OpaqueGeometry = 1 << 2,

    /// <summary>HDR opaque lighting and material evaluation.</summary>
    HdrOpaqueLighting = 1 << 3,

    /// <summary>Emissive contribution accumulation.</summary>
    Emission = 1 << 4,

    /// <summary>Sorted or weighted transparent surface composition.</summary>
    Transparency = 1 << 5,

    /// <summary>Decal projection and particle layer composition.</summary>
    DecalsAndParticles = 1 << 6,

    /// <summary>Volumetric density, scattering, and light integration.</summary>
    Volumetrics = 1 << 7,

    /// <summary>Bloom extraction, downsample, blur, and composition.</summary>
    Bloom = 1 << 8,

    /// <summary>Tone mapping, grading, distortion, and output encoding.</summary>
    ToneMapping = 1 << 9,

    /// <summary>Diagnostic overlays and host presentation.</summary>
    DiagnosticsAndPresentation = 1 << 10,

    /// <summary>Every defined pipeline stage.</summary>
    All = Preparation
        | DepthVisibility
        | OpaqueGeometry
        | HdrOpaqueLighting
        | Emission
        | Transparency
        | DecalsAndParticles
        | Volumetrics
        | Bloom
        | ToneMapping
        | DiagnosticsAndPresentation,
}
