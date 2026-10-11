namespace Novolis.Rendering.FrameGraph;

/// <summary>Names the renderer-owned stages in the standard frame pipeline.</summary>
public enum FrameGraphPipelineStage
{
    /// <summary>Camera, view, scene, and visibility preparation.</summary>
    Preparation = 0,
    /// <summary>Alias for <see cref="Preparation"/>.</summary>
    ScenePreparation = Preparation,

    /// <summary>Depth and visibility generation for opaque geometry.</summary>
    DepthVisibility = 1,
    /// <summary>Alias for <see cref="DepthVisibility"/>.</summary>
    DepthAndVisibility = DepthVisibility,

    /// <summary>Opaque geometry submission before lighting.</summary>
    OpaqueGeometry = 2,
    /// <summary>Alias for <see cref="OpaqueGeometry"/>.</summary>
    Opaque = OpaqueGeometry,

    /// <summary>HDR opaque lighting and material evaluation.</summary>
    HdrOpaqueLighting = 3,
    /// <summary>Alias for <see cref="HdrOpaqueLighting"/>.</summary>
    HdrLighting = HdrOpaqueLighting,

    /// <summary>Emissive contribution accumulation.</summary>
    Emission = 4,

    /// <summary>Sorted or weighted transparent surface composition.</summary>
    Transparency = 5,

    /// <summary>Decal projection and particle layer composition.</summary>
    DecalsAndParticles = 6,
    /// <summary>Short alias for <see cref="DecalsAndParticles"/>.</summary>
    DecalsParticles = DecalsAndParticles,

    /// <summary>Volumetric density, scattering, and light integration.</summary>
    Volumetrics = 7,

    /// <summary>Bloom extraction, downsample, blur, and composition.</summary>
    Bloom = 8,

    /// <summary>Tone mapping, grading, distortion, and output encoding.</summary>
    ToneMapping = 9,

    /// <summary>Diagnostic overlays and host presentation.</summary>
    DiagnosticsAndPresentation = 10,
    /// <summary>Short alias for <see cref="DiagnosticsAndPresentation"/>.</summary>
    DiagnosticsPresentation = DiagnosticsAndPresentation,
}
