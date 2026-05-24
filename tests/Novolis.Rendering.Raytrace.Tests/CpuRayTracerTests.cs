using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;
using Novolis.Rendering.Raytrace;
using TUnit.Core;

namespace Novolis.Rendering.Raytrace.Tests;

public sealed class CpuRayTracerTests
{
    [Test]
    public async Task Render_UnitCubeRoom_HitsGeometry()
    {
        var tracer = new CpuRayTracer();
        var buffer = new ImageBuffer(64, 48);
        var camera = RenderCamera.LookAt(
            new Vector3(1.2f, 0.8f, 2f),
            new Vector3(0f, 0.2f, 0f),
            Vector3.UnitY,
            55f,
            buffer.Width / (float)buffer.Height);

        tracer.Render(buffer, camera, RenderSceneFactory.UnitCubeRoom());

        var center = buffer[buffer.Width / 2, buffer.Height / 2];
        await Assert.That(center).IsNotEqualTo(Rgba32.Black);
        await Assert.That((int)center.R).IsGreaterThan(30);
    }

    [Test]
    public async Task Render_EmptyScene_SkyGradient()
    {
        var tracer = new CpuRayTracer();
        var buffer = new ImageBuffer(8, 8);
        var camera = RenderCamera.LookAt(
            Vector3.UnitZ * 3f,
            Vector3.Zero,
            Vector3.UnitY,
            60f,
            1f);

        tracer.Render(buffer, camera, new RenderScene());

        await Assert.That(buffer[4, 4]).IsNotEqualTo(Rgba32.Black);
        await Assert.That((int)buffer[4, 7].B).IsNotEqualTo((int)buffer[4, 0].B);
    }
}
