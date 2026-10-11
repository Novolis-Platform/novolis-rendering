using Novolis.Rendering.FrameGraph;
using TUnit.Core;

namespace Novolis.Rendering.FrameGraph.Tests;

public sealed class FrameGraphContractTests
{
    [Test]
    public async Task Compile_InfersOrderLifetimesAndBarriers()
    {
        var builder = new FrameGraphBuilder("Lighting", new FrameGraphSize(1280, 720));
        var hdr = builder.CreateTexture(
            "Hdr",
            FrameGraphTextureDescription.Output(
                FrameGraphFormat.Rgba16Float,
                FrameGraphResourceUsage.ColorAttachment | FrameGraphResourceUsage.Sampled));
        var output = builder.CreateTexture(
            "Output",
            FrameGraphTextureDescription.Output(
                FrameGraphFormat.Rgba8Srgb,
                FrameGraphResourceUsage.ColorAttachment | FrameGraphResourceUsage.Present));

        var opaque = builder.AddPass(
            "Opaque",
            FrameGraphPassKind.Raster,
            pass => pass.WriteColor(hdr));
        var toneMap = builder.AddPass(
            "ToneMap",
            FrameGraphPassKind.PostProcess,
            pass => pass.Read(hdr).WriteColor(output).Require(FrameGraphTargetFeature.ToneMapping));

        var plan = builder.Compile(FrameGraphQualityProfile.High);

        await Assert.That(plan.Passes.Count).IsEqualTo(2);
        await Assert.That(plan.Passes[0].Description.Handle).IsEqualTo(opaque);
        await Assert.That(plan.Passes[1].Description.Handle).IsEqualTo(toneMap);
        await Assert.That(plan.GetResource(hdr).FirstUseIndex).IsEqualTo(0);
        await Assert.That(plan.GetResource(hdr).LastUseIndex).IsEqualTo(1);
        await Assert.That(plan.Barriers.Count).IsGreaterThanOrEqualTo(3);
        await Assert.That(
                plan.Barriers.Any(
                    barrier =>
                        barrier.Resource == (FrameGraphResourceHandle)hdr
                        && barrier.Before == FrameGraphResourceState.ColorAttachment
                        && barrier.After == FrameGraphResourceState.ShaderRead))
            .IsTrue();
    }

    [Test]
    public async Task Validate_RejectsTransientReadBeforeWrite()
    {
        var builder = new FrameGraphBuilder(new FrameGraphSize(64, 64));
        var texture = builder.CreateTexture(
            "Transient",
            FrameGraphTextureDescription.Output(
                FrameGraphFormat.Rgba8Unorm,
                FrameGraphResourceUsage.Sampled | FrameGraphResourceUsage.ColorAttachment));
        builder.AddPass("ReadFirst", pass => pass.Read(texture));

        var validation = builder.Build().Validate();

        await Assert.That(validation.IsValid).IsFalse();
        await Assert.That(validation.Diagnostics.Any(diagnostic => diagnostic.Code == "FG015")).IsTrue();
    }

    [Test]
    public async Task Resize_UsesQualityScaleAndCpuTargetCapturesDeterministically()
    {
        var builder = new FrameGraphBuilder("Resize", new FrameGraphSize(1280, 720));
        var color = builder.CreateTexture(
            "Color",
            FrameGraphTextureDescription.Output(
                FrameGraphFormat.Rgba16Float,
                FrameGraphResourceUsage.ColorAttachment | FrameGraphResourceUsage.Present));
        builder.AddPass(
            "Present",
            FrameGraphPassKind.Present,
            pass => pass.WriteColor(color).Require(FrameGraphTargetFeature.Hdr));

        var plan = builder.Compile(FrameGraphQualityProfile.Medium);
        var resized = plan.Resize(new FrameGraphSize(1920, 1080));
        var target = new CpuReferenceFrameGraphTarget(resized.OutputSize);
        var execution = target.Execute(resized);
        var firstCapture = resized.CaptureDiagnostics().ToJson();
        var secondCapture = FrameGraphCapture.Capture(resized).ToJson();

        await Assert.That(resized.RenderSize).IsEqualTo(new FrameGraphSize(1440, 810));
        await Assert.That(resized.GetResource(color).ResolvedSize).IsEqualTo(new FrameGraphSize(1440, 810));
        await Assert.That(execution.Succeeded).IsTrue();
        await Assert.That(execution.ExecutedPassCount).IsEqualTo(1);
        await Assert.That(firstCapture).IsEqualTo(secondCapture);
    }

    [Test]
    public async Task TryCompile_ReportsUnsupportedTargetFeature()
    {
        var builder = new FrameGraphBuilder(new FrameGraphSize(32, 32));
        var color = builder.CreateTexture(
            "Color",
            FrameGraphTextureDescription.Output(
                FrameGraphFormat.Rgba16Float,
                FrameGraphResourceUsage.ColorAttachment));
        builder.AddPass(
            "Bloom",
            FrameGraphPassKind.PostProcess,
            pass => pass.WriteColor(color).Require(FrameGraphTargetFeature.Bloom));

        var result = FrameGraphCompiler.TryCompile(
            builder.Build(),
            new FrameGraphCompileOptions
            {
                TargetCapabilities = new FrameGraphTargetCapabilities(
                    "No Bloom",
                    FrameGraphTargetFeature.Hdr),
            });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "FG022")).IsTrue();
    }

    [Test]
    public async Task FullPipelineBuilder_ProducesStableStageOrderAndCapture()
    {
        var quality = FrameGraphQualityProfile.High;
        var first = FrameGraphPipelineBuilder.Build(
            new FrameGraphSize(320, 200),
            quality,
            "Full");
        var second = FrameGraphPipelineBuilder.Build(
            new FrameGraphSize(320, 200),
            quality,
            "Full");

        var options = new FrameGraphCompileOptions
        {
            QualityProfile = quality,
            TargetDescription = FrameGraphTargetDescription.CpuReference,
        };
        var firstPlan = FrameGraphCompiler.Compile(first, options);
        var secondPlan = FrameGraphCompiler.Compile(second, options);

        await Assert.That(firstPlan.Passes.Count).IsEqualTo(14);
        await Assert.That(
                firstPlan.Passes.Select(pass => pass.Description.Stage))
            .IsEquivalentTo(
            [
                FrameGraphPipelineStage.Preparation,
                FrameGraphPipelineStage.DepthVisibility,
                FrameGraphPipelineStage.HdrOpaqueLighting,
                FrameGraphPipelineStage.Emission,
                FrameGraphPipelineStage.Transparency,
                FrameGraphPipelineStage.DecalsAndParticles,
                FrameGraphPipelineStage.DecalsAndParticles,
                FrameGraphPipelineStage.Volumetrics,
                FrameGraphPipelineStage.Bloom,
                FrameGraphPipelineStage.Bloom,
                FrameGraphPipelineStage.Bloom,
                FrameGraphPipelineStage.ToneMapping,
                FrameGraphPipelineStage.DiagnosticsAndPresentation,
                FrameGraphPipelineStage.DiagnosticsAndPresentation,
            ]);
        await Assert.That(firstPlan.CaptureDiagnostics().ToDeterministicText())
            .IsEqualTo(secondPlan.CaptureDiagnostics().ToDeterministicText());
        await Assert.That(firstPlan.TargetDescription).IsEqualTo(FrameGraphTargetDescription.CpuReference);
    }

    [Test]
    public async Task TargetDescriptions_ExposeTruthfulBackendBoundaries()
    {
        var pathTrace = FrameGraphTargetDescription.PathTraceCpu;
        var raster = FrameGraphTargetDescription.RasterNeutral;
        var planar = FrameGraphTargetDescription.PlanarNeutral;

        await Assert.That(pathTrace.Capabilities.SupportsPbr).IsTrue();
        await Assert.That(pathTrace.Capabilities.SupportsShadows).IsTrue();
        await Assert.That(pathTrace.Capabilities.SupportsTransparency).IsTrue();
        await Assert.That(pathTrace.Capabilities.SupportsBloom).IsFalse();
        await Assert.That(pathTrace.Capabilities.SupportsVolumetrics).IsFalse();
        await Assert.That(pathTrace.ClaimsExecution).IsTrue();
        await Assert.That(raster.ClaimsExecution).IsFalse();
        await Assert.That(planar.ClaimsExecution).IsFalse();
        await Assert.That(raster.Capabilities.Features).IsEqualTo(FrameGraphTargetFeature.None);
        await Assert.That(planar.Capabilities.Features).IsEqualTo(FrameGraphTargetFeature.None);
    }

    [Test]
    public async Task StageAndFeatureRequirements_RejectUnsupportedTargets()
    {
        var graph = FrameGraphPipelineBuilder.Build(
            new FrameGraphSize(64, 64),
            FrameGraphQualityProfile.High);
        var result = FrameGraphCompiler.TryCompile(
            graph,
            new FrameGraphCompileOptions
            {
                QualityProfile = FrameGraphQualityProfile.High,
                TargetDescription = FrameGraphTargetDescription.PathTraceCpu,
            });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "FG022")).IsTrue();
        await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "FG034")).IsTrue();
    }
}
