namespace Novolis.Rendering.FrameGraph;

/// <summary>Conversions between typed pipeline stages and capability masks.</summary>
public static class FrameGraphPipelineStageExtensions
{
    /// <summary>Converts a pipeline stage to its capability mask.</summary>
    public static FrameGraphPipelineStageMask ToMask(this FrameGraphPipelineStage stage) =>
        stage switch
        {
            FrameGraphPipelineStage.Preparation => FrameGraphPipelineStageMask.Preparation,
            FrameGraphPipelineStage.DepthVisibility => FrameGraphPipelineStageMask.DepthVisibility,
            FrameGraphPipelineStage.OpaqueGeometry => FrameGraphPipelineStageMask.OpaqueGeometry,
            FrameGraphPipelineStage.HdrOpaqueLighting => FrameGraphPipelineStageMask.HdrOpaqueLighting,
            FrameGraphPipelineStage.Emission => FrameGraphPipelineStageMask.Emission,
            FrameGraphPipelineStage.Transparency => FrameGraphPipelineStageMask.Transparency,
            FrameGraphPipelineStage.DecalsAndParticles => FrameGraphPipelineStageMask.DecalsAndParticles,
            FrameGraphPipelineStage.Volumetrics => FrameGraphPipelineStageMask.Volumetrics,
            FrameGraphPipelineStage.Bloom => FrameGraphPipelineStageMask.Bloom,
            FrameGraphPipelineStage.ToneMapping => FrameGraphPipelineStageMask.ToneMapping,
            FrameGraphPipelineStage.DiagnosticsAndPresentation => FrameGraphPipelineStageMask.DiagnosticsAndPresentation,
            _ => FrameGraphPipelineStageMask.None,
        };

    /// <summary>Converts a compatibility pass stage to a pipeline stage.</summary>
    public static FrameGraphPipelineStage ToPipelineStage(this FrameGraphPassStage stage) =>
        (FrameGraphPipelineStage)stage;

    /// <summary>Returns the conventional pass kind for a pipeline stage.</summary>
    public static FrameGraphPassKind ToPassKind(this FrameGraphPipelineStage stage) =>
        stage switch
        {
            FrameGraphPipelineStage.Preparation => FrameGraphPassKind.Preparation,
            FrameGraphPipelineStage.DepthVisibility => FrameGraphPassKind.Raster,
            FrameGraphPipelineStage.OpaqueGeometry => FrameGraphPassKind.Raster,
            FrameGraphPipelineStage.HdrOpaqueLighting => FrameGraphPassKind.Compute,
            FrameGraphPipelineStage.Emission => FrameGraphPassKind.Compute,
            FrameGraphPipelineStage.Transparency => FrameGraphPassKind.Raster,
            FrameGraphPipelineStage.DecalsAndParticles => FrameGraphPassKind.Raster,
            FrameGraphPipelineStage.Volumetrics => FrameGraphPassKind.Compute,
            FrameGraphPipelineStage.Bloom => FrameGraphPassKind.PostProcess,
            FrameGraphPipelineStage.ToneMapping => FrameGraphPassKind.PostProcess,
            FrameGraphPipelineStage.DiagnosticsAndPresentation => FrameGraphPassKind.Diagnostic,
            _ => FrameGraphPassKind.Compute,
        };

    /// <summary>Returns the conventional stage for a legacy pass kind.</summary>
    public static FrameGraphPipelineStage ToPipelineStage(this FrameGraphPassKind kind) =>
        kind switch
        {
            FrameGraphPassKind.Preparation => FrameGraphPipelineStage.Preparation,
            FrameGraphPassKind.Raster => FrameGraphPipelineStage.OpaqueGeometry,
            FrameGraphPassKind.Compute => FrameGraphPipelineStage.HdrOpaqueLighting,
            FrameGraphPassKind.Copy => FrameGraphPipelineStage.ToneMapping,
            FrameGraphPassKind.Resolve => FrameGraphPipelineStage.ToneMapping,
            FrameGraphPassKind.PostProcess => FrameGraphPipelineStage.ToneMapping,
            FrameGraphPassKind.Present => FrameGraphPipelineStage.DiagnosticsAndPresentation,
            FrameGraphPassKind.Diagnostic => FrameGraphPipelineStage.DiagnosticsAndPresentation,
            _ => FrameGraphPipelineStage.Preparation,
        };
}
