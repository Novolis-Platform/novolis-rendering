namespace Novolis.Rendering.FrameGraph;

/// <summary>Compatibility name for callers that refer to a pass stage.</summary>
public enum FrameGraphPassStage
{
    /// <summary>Camera, view, scene, and visibility preparation.</summary>
    Preparation = FrameGraphPipelineStage.Preparation,
    /// <summary>Alias for <see cref="Preparation"/>.</summary>
    ScenePreparation = Preparation,

    /// <summary>Depth and visibility generation.</summary>
    DepthVisibility = FrameGraphPipelineStage.DepthVisibility,
    /// <summary>Alias for <see cref="DepthVisibility"/>.</summary>
    DepthAndVisibility = DepthVisibility,

    /// <summary>Opaque geometry.</summary>
    OpaqueGeometry = FrameGraphPipelineStage.OpaqueGeometry,
    /// <summary>Alias for <see cref="OpaqueGeometry"/>.</summary>
    Opaque = OpaqueGeometry,

    /// <summary>HDR opaque lighting.</summary>
    HdrOpaqueLighting = FrameGraphPipelineStage.HdrOpaqueLighting,
    /// <summary>Alias for <see cref="HdrOpaqueLighting"/>.</summary>
    HdrLighting = HdrOpaqueLighting,

    /// <summary>Emission.</summary>
    Emission = FrameGraphPipelineStage.Emission,

    /// <summary>Transparency.</summary>
    Transparency = FrameGraphPipelineStage.Transparency,

    /// <summary>Decals and particles.</summary>
    DecalsAndParticles = FrameGraphPipelineStage.DecalsAndParticles,
    /// <summary>Short alias for <see cref="DecalsAndParticles"/>.</summary>
    DecalsParticles = DecalsAndParticles,

    /// <summary>Volumetrics.</summary>
    Volumetrics = FrameGraphPipelineStage.Volumetrics,

    /// <summary>Bloom.</summary>
    Bloom = FrameGraphPipelineStage.Bloom,

    /// <summary>Tone mapping.</summary>
    ToneMapping = FrameGraphPipelineStage.ToneMapping,

    /// <summary>Diagnostics and presentation.</summary>
    DiagnosticsAndPresentation = FrameGraphPipelineStage.DiagnosticsAndPresentation,
    /// <summary>Short alias for <see cref="DiagnosticsAndPresentation"/>.</summary>
    DiagnosticsPresentation = DiagnosticsAndPresentation,
}
