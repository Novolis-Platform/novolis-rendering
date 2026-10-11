namespace Novolis.Rendering.FrameGraph;

/// <summary>
/// Builds the standard renderer-owned frame pipeline with stable resource and
/// pass names.
/// </summary>
/// <remarks>
/// This builder only creates descriptions. It does not invoke a scene compiler,
/// a ray-tracing backend, a raster host, or a presentation API.
/// </remarks>
public static class FrameGraphPipelineBuilder
{
    /// <summary>Builds the complete pipeline description for a quality profile.</summary>
    public static FrameGraphDescription Build(
        FrameGraphSize outputSize,
        FrameGraphQualityProfile? qualityProfile = null,
        string name = "Frame")
    {
        return Create(outputSize, qualityProfile, name).Build();
    }

    /// <summary>Alias for callers that want to emphasize the standard full pipeline.</summary>
    public static FrameGraphDescription BuildFull(
        FrameGraphSize outputSize,
        FrameGraphQualityProfile? qualityProfile = null,
        string name = "Frame") =>
        Build(outputSize, qualityProfile, name);

    /// <summary>Creates a mutable builder for the standard pipeline.</summary>
    public static FrameGraphBuilder Create(
        FrameGraphSize outputSize,
        FrameGraphQualityProfile? qualityProfile = null,
        string name = "Frame")
    {
        var quality = qualityProfile ?? FrameGraphQualityProfile.Default;
        var builder = new FrameGraphBuilder(name, outputSize);
        var sceneData = builder.CreateBuffer(
            "SceneData",
            new FrameGraphBufferDescription(
                256,
                FrameGraphResourceUsage.StorageRead | FrameGraphResourceUsage.StorageWrite));

        var depth = quality.IsEnabled(FrameGraphTargetFeature.Depth)
            ? builder.CreateTexture(
                "Depth",
                FrameGraphTextureDescription.Output(
                    FrameGraphFormat.D32Float,
                    FrameGraphResourceUsage.DepthStencilAttachment | FrameGraphResourceUsage.Sampled))
            : (FrameGraphTextureHandle?)null;

        var hdrEnabled = quality.IsEnabled(FrameGraphTargetFeature.Hdr);
        var sceneColor = builder.CreateTexture(
            "SceneColor",
            FrameGraphTextureDescription.Output(
                hdrEnabled ? FrameGraphFormat.Rgba16Float : FrameGraphFormat.Rgba8Srgb,
                FrameGraphResourceUsage.ColorAttachment
                    | FrameGraphResourceUsage.Sampled
                    | FrameGraphResourceUsage.StorageRead
                    | FrameGraphResourceUsage.StorageWrite));
        var output = builder.CreateTexture(
            "Output",
            FrameGraphTextureDescription.Output(
                FrameGraphFormat.Rgba8Srgb,
                FrameGraphResourceUsage.ColorAttachment
                    | FrameGraphResourceUsage.Sampled
                    | FrameGraphResourceUsage.StorageRead
                    | FrameGraphResourceUsage.StorageWrite
                    | FrameGraphResourceUsage.Present));

        AddPreparation(builder, quality, sceneData);
        AddDepthVisibility(builder, quality, sceneData, depth);
        AddOpaqueLighting(builder, quality, sceneData, depth, sceneColor, hdrEnabled);
        AddEmission(builder, quality, sceneColor);
        AddTransparency(builder, quality, sceneData, depth, sceneColor);
        AddDecalsAndParticles(builder, quality, sceneData, depth, sceneColor);
        AddVolumetrics(builder, quality, depth, sceneColor);

        FrameGraphTextureHandle? bloom = null;
        if (quality.IsEnabled(FrameGraphTargetFeature.Bloom)
            && quality.IsStageEnabled(FrameGraphPipelineStage.Bloom))
        {
            bloom = builder.CreateTexture(
                "Bloom",
                FrameGraphTextureDescription.Scaled(
                    FrameGraphFormat.Rgba16Float,
                    FrameGraphResourceUsage.ColorAttachment
                        | FrameGraphResourceUsage.Sampled
                        | FrameGraphResourceUsage.StorageRead
                        | FrameGraphResourceUsage.StorageWrite,
                    0.5f,
                    mipLevels: Math.Max(1, quality.BloomLevels)));
            AddBloom(builder, quality, sceneColor, bloom.Value, hdrEnabled);
        }

        AddToneMapping(builder, quality, sceneColor, output);
        AddDiagnosticsAndPresentation(builder, quality, output);
        return builder;
    }

    /// <summary>Builds and compiles the standard pipeline against a target.</summary>
    public static CompiledFrameGraph Compile(
        FrameGraphSize outputSize,
        FrameGraphTargetDescription target,
        FrameGraphQualityProfile? qualityProfile = null,
        string name = "Frame")
    {
        ArgumentNullException.ThrowIfNull(target);
        var quality = qualityProfile ?? FrameGraphQualityProfile.Default;
        return Build(outputSize, quality, name).CompileForTarget(target, quality);
    }

    private static void AddPreparation(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphBufferHandle sceneData)
    {
        if (!quality.IsStageEnabled(FrameGraphPipelineStage.Preparation))
        {
            return;
        }

        builder.AddPass(
            "PrepareScene",
            FrameGraphPipelineStage.Preparation,
            pass => pass.Write(sceneData, FrameGraphResourceUsage.StorageWrite));
    }

    private static void AddDepthVisibility(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphBufferHandle sceneData,
        FrameGraphTextureHandle? depth)
    {
        if (depth is not { } depthTexture
            || !quality.IsStageEnabled(FrameGraphPipelineStage.DepthVisibility))
        {
            return;
        }

        builder.AddPass(
            "DepthVisibility",
            FrameGraphPipelineStage.DepthVisibility,
            pass => pass
                .Read(sceneData, FrameGraphResourceUsage.StorageRead)
                .WriteDepth(depthTexture)
                .Require(FrameGraphTargetFeature.Depth));
    }

    private static void AddOpaqueLighting(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphBufferHandle sceneData,
        FrameGraphTextureHandle? depth,
        FrameGraphTextureHandle sceneColor,
        bool hdrEnabled)
    {
        var stage = hdrEnabled
            ? FrameGraphPipelineStage.HdrOpaqueLighting
            : FrameGraphPipelineStage.OpaqueGeometry;
        if (!quality.IsStageEnabled(stage))
        {
            return;
        }

        builder.AddPass(
            "OpaqueLighting",
            stage,
            pass =>
            {
                pass.Read(sceneData, FrameGraphResourceUsage.StorageRead);
                if (depth is { } depthTexture)
                {
                    pass.Read(depthTexture, FrameGraphResourceUsage.Sampled);
                }

                pass.WriteColor(sceneColor);
                pass.Require(FrameGraphTargetFeature.Pbr);
                if (hdrEnabled)
                {
                    pass.Require(FrameGraphTargetFeature.Hdr);
                }
            });
    }

    private static void AddEmission(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphTextureHandle sceneColor)
    {
        if (!quality.IsEnabled(FrameGraphTargetFeature.Emission)
            || !quality.IsStageEnabled(FrameGraphPipelineStage.Emission))
        {
            return;
        }

        builder.AddPass(
            "Emission",
            FrameGraphPipelineStage.Emission,
            pass => pass
                .ReadWrite(sceneColor, FrameGraphResourceUsage.Storage)
                .Require(FrameGraphTargetFeature.Emission));
    }

    private static void AddTransparency(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphBufferHandle sceneData,
        FrameGraphTextureHandle? depth,
        FrameGraphTextureHandle sceneColor)
    {
        if (!quality.IsEnabled(FrameGraphTargetFeature.Transparency)
            || !quality.IsStageEnabled(FrameGraphPipelineStage.Transparency))
        {
            return;
        }

        builder.AddPass(
            "Transparency",
            FrameGraphPipelineStage.Transparency,
            pass =>
            {
                pass.Read(sceneData, FrameGraphResourceUsage.StorageRead);
                if (depth is { } depthTexture)
                {
                    pass.Read(depthTexture, FrameGraphResourceUsage.Sampled);
                }

                pass.ReadWrite(sceneColor, FrameGraphResourceUsage.Storage);
                pass.Require(FrameGraphTargetFeature.Transparency);
            });
    }

    private static void AddDecalsAndParticles(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphBufferHandle sceneData,
        FrameGraphTextureHandle? depth,
        FrameGraphTextureHandle sceneColor)
    {
        if (!quality.IsStageEnabled(FrameGraphPipelineStage.DecalsAndParticles))
        {
            return;
        }

        if (quality.IsEnabled(FrameGraphTargetFeature.Decals))
        {
            builder.AddPass(
                "Decals",
                FrameGraphPipelineStage.DecalsAndParticles,
                pass =>
                {
                    pass.Read(sceneData, FrameGraphResourceUsage.StorageRead);
                    if (depth is { } depthTexture)
                    {
                        pass.Read(depthTexture, FrameGraphResourceUsage.Sampled);
                    }

                    pass.ReadWrite(sceneColor, FrameGraphResourceUsage.Storage);
                    pass.Require(FrameGraphTargetFeature.Decals);
                });
        }

        if (quality.IsEnabled(FrameGraphTargetFeature.Particles))
        {
            builder.AddPass(
                "Particles",
                FrameGraphPipelineStage.DecalsAndParticles,
                pass => pass
                    .Read(sceneData, FrameGraphResourceUsage.StorageRead)
                    .ReadWrite(sceneColor, FrameGraphResourceUsage.Storage)
                    .Require(FrameGraphTargetFeature.Particles));
        }
    }

    private static void AddVolumetrics(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphTextureHandle? depth,
        FrameGraphTextureHandle sceneColor)
    {
        if (!quality.IsEnabled(FrameGraphTargetFeature.Volumetrics)
            || !quality.IsStageEnabled(FrameGraphPipelineStage.Volumetrics))
        {
            return;
        }

        builder.AddPass(
            "Volumetrics",
            FrameGraphPipelineStage.Volumetrics,
            pass =>
            {
                if (depth is { } depthTexture)
                {
                    pass.Read(depthTexture, FrameGraphResourceUsage.Sampled);
                }

                pass.ReadWrite(sceneColor, FrameGraphResourceUsage.Storage);
                pass.Require(FrameGraphTargetFeature.Volumetrics);
            });
    }

    private static void AddBloom(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphTextureHandle sceneColor,
        FrameGraphTextureHandle bloom,
        bool hdrEnabled)
    {
        builder.AddPass(
            "BloomExtract",
            FrameGraphPipelineStage.Bloom,
            pass =>
            {
                pass.Read(sceneColor, FrameGraphResourceUsage.Sampled);
                pass.WriteColor(bloom);
                pass.Require(FrameGraphTargetFeature.Bloom);
                if (hdrEnabled)
                {
                    pass.Require(FrameGraphTargetFeature.Hdr);
                }
            });
        builder.AddPass(
            "BloomBlur",
            FrameGraphPipelineStage.Bloom,
            pass => pass
                .ReadWrite(bloom, FrameGraphResourceUsage.Storage)
                .Require(FrameGraphTargetFeature.Bloom));
        builder.AddPass(
            "BloomComposite",
            FrameGraphPipelineStage.Bloom,
            pass => pass
                .Read(bloom, FrameGraphResourceUsage.Sampled)
                .ReadWrite(sceneColor, FrameGraphResourceUsage.Storage)
                .Require(FrameGraphTargetFeature.Bloom));
    }

    private static void AddToneMapping(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphTextureHandle sceneColor,
        FrameGraphTextureHandle output)
    {
        if (!quality.IsStageEnabled(FrameGraphPipelineStage.ToneMapping))
        {
            return;
        }

        if (quality.IsEnabled(FrameGraphTargetFeature.ToneMapping))
        {
            builder.AddPass(
                "ToneMapping",
                FrameGraphPipelineStage.ToneMapping,
                pass => pass
                    .Read(sceneColor, FrameGraphResourceUsage.Sampled)
                    .WriteColor(output)
                    .Require(FrameGraphTargetFeature.ToneMapping));
            return;
        }

        builder.AddPass(
            "OutputEncode",
            FrameGraphPipelineStage.ToneMapping,
            pass => pass
                .Read(sceneColor, FrameGraphResourceUsage.Sampled)
                .WriteColor(output));
    }

    private static void AddDiagnosticsAndPresentation(
        FrameGraphBuilder builder,
        FrameGraphQualityProfile quality,
        FrameGraphTextureHandle output)
    {
        if (!quality.IsStageEnabled(FrameGraphPipelineStage.DiagnosticsAndPresentation))
        {
            return;
        }

        if (quality.IsEnabled(FrameGraphTargetFeature.Diagnostics))
        {
            builder.AddPass(
                "DiagnosticsOverlay",
                FrameGraphPipelineStage.DiagnosticsAndPresentation,
                pass => pass
                    .ReadWrite(
                        output,
                        FrameGraphResourceUsage.ColorAttachment
                            | FrameGraphResourceUsage.Sampled
                            | FrameGraphResourceUsage.Present)
                    .Require(FrameGraphTargetFeature.Diagnostics));
        }

        if (quality.IsEnabled(FrameGraphTargetFeature.Presentation))
        {
            builder.AddPass(
                "Present",
                FrameGraphPipelineStage.DiagnosticsAndPresentation,
                pass => pass
                    .Read(output, FrameGraphResourceUsage.Present)
                    .Require(FrameGraphTargetFeature.Presentation));
        }
    }
}
