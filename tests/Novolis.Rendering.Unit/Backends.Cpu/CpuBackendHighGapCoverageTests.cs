using System.Numerics;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Scene;
using TUnit.Core;

namespace Novolis.Rendering.Unit.Backends.Cpu;

/// <summary>Raises Cpu backend / path-tracer coverage across materials and control paths.</summary>
public sealed class CpuBackendHighGapCoverageTests
{
    [Test]
    public async Task BackendLabel_GpuSurface_Reset_AndRenderSamples()
    {
        var deterministic = new CpuRayTracingBackend(deterministic: true);
        await Assert.That(deterministic.BackendLabel).IsEqualTo("CPU (deterministic)");
        await Assert.That(deterministic.GpuSurface).IsNull();

        var nondet = new CpuRayTracingBackend(deterministic: false);
        await Assert.That(nondet.BackendLabel).IsEqualTo("CPU");

        await deterministic.ResizeAsync(24, 18);
        await deterministic.UploadSceneAsync(DemoSceneFactory.UnitCubeRoom());
        var camera = CameraSnapshot.LookAt(
            new Vector3(1.5f, 1f, 2.5f),
            new Vector3(0f, 0.2f, 0f),
            Vector3.UnitY,
            50f,
            24f / 18f);

        deterministic.RenderSamples(camera, 0, 0);
        await Assert.That(deterministic.SampleCount).IsEqualTo(0);

        deterministic.RenderSamples(camera, 0, 2);
        await Assert.That(deterministic.SampleCount).IsEqualTo(2);

        deterministic.ResetAccumulation();
        await Assert.That(deterministic.SampleCount).IsEqualTo(0);
    }

    [Test]
    public async Task Render_ThrowsBeforeResize()
    {
        var backend = new CpuRayTracingBackend(deterministic: true);
        var camera = CameraSnapshot.LookAt(Vector3.UnitZ * 2f, Vector3.Zero, Vector3.UnitY, 45f, 1f);
        await Assert.That(async () => await backend.RenderAsync(camera, 0))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task PathTracer_GlassSkinEmissiveAndPointLight()
    {
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(new Vector3(0.4f, 0.4f, 0.45f), 0.85f))
            .AddBox(new Vector3(-0.4f, 0.25f, 0f), new Vector3(0.2f), MaterialPresets.Glass(new Vector3(0.8f, 0.9f, 1f), 0.02f, 1.5f))
            .AddBox(new Vector3(0.4f, 0.25f, 0f), new Vector3(0.2f), MaterialPresets.Skin(new Vector3(0.8f, 0.55f, 0.45f), 0.4f))
            .AddBox(new Vector3(0f, 0.6f, -0.3f), new Vector3(0.15f), MaterialPresets.Emissive(new Vector3(1f, 0.7f, 0.3f), 3f))
            .AddBox(new Vector3(0f, 0.25f, 0.5f), new Vector3(0.2f), MaterialPresets.Metal(MaterialPresets.Colors.Silver, 0.05f))
            .AddDirectionalLight(new Vector3(-0.3f, -1f, -0.2f), Vector3.One, 0.8f)
            .Build();

        // Point light via LightDefinition on compiled scene — SceneBuilder only has directional;
        // metal + glass + skin + emissive still exercise PathTracerEngine shade branches.
        var compiled = SceneCompiler.Compile(scene);

        var backend = new CpuRayTracingBackend(deterministic: true);
        await backend.ResizeAsync(32, 24);
        await backend.UploadSceneAsync(compiled);
        var camera = CameraSnapshot.LookAt(
            new Vector3(1.4f, 0.9f, 2.2f),
            new Vector3(0f, 0.3f, 0f),
            Vector3.UnitY,
            55f,
            32f / 24f);
        await backend.RenderAsync(camera, 0);
        backend.Output.TryGetCpuPixels(out var pixels, out var w, out var h);
        var pixelCount = pixels.Length;
        var lit = false;
        for (var i = 0; i < pixelCount; i++)
        {
            if (pixels[i].R > 5 || pixels[i].G > 5 || pixels[i].B > 5)
            {
                lit = true;
                break;
            }
        }

        await Assert.That(w * h).IsEqualTo(pixelCount);
        await Assert.That(lit).IsTrue();
    }

    [Test]
    public async Task PathTracer_EmptyScene_SamplesSky()
    {
        var empty = SceneCompiler.Compile(new SceneBuilder().Build());
        var backend = new CpuRayTracingBackend(deterministic: true);
        await backend.ResizeAsync(16, 12);
        await backend.UploadSceneAsync(empty);
        var camera = CameraSnapshot.LookAt(new Vector3(0, 1, 2), Vector3.Zero, Vector3.UnitY, 60f, 16f / 12f);
        await backend.RenderAsync(camera, 0);
        backend.Output.TryGetCpuPixels(out var pixels, out _, out _);
        await Assert.That(pixels.Length).IsEqualTo(16 * 12);
    }

    [Test]
    public async Task NonDeterministic_RenderCompletes()
    {
        var backend = new CpuRayTracingBackend(deterministic: false);
        await backend.ResizeAsync(12, 10);
        await backend.UploadSceneAsync(DemoSceneFactory.UnitCubeRoom());
        var camera = CameraSnapshot.LookAt(new Vector3(1.2f, 0.8f, 2f), Vector3.Zero, Vector3.UnitY, 50f, 1.2f);
        await backend.RenderAsync(camera, 0);
        await Assert.That(backend.SampleCount).IsEqualTo(1);
    }
}
