using Novolis.Rendering.PathTrace.Demos;
using TUnit.Core;

namespace Novolis.Rendering.PathTrace.Demos.Tests;

public sealed class ShowcaseScenesTests
{
    [Test]
    public async Task BuildHelloShowcase_has_meshes()
    {
        var scene = ShowcaseScenes.BuildHelloShowcase();
        await Assert.That(scene.Triangles.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task BuildStudioShowcase_extracts_emissive_point_lights()
    {
        var studio = ShowcaseScenes.BuildStudioShowcase();
        var pointLights = 0;
        foreach (var light in studio.Lights)
        {
            if (light.Kind == Novolis.Rendering.Runtime.GpuLightKind.Point)
            {
                pointLights++;
            }
        }

        await Assert.That(pointLights).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task BuildStudioShowcase_is_a_dense_diorama()
    {
        var hello = ShowcaseScenes.BuildHelloShowcase();
        var studio = ShowcaseScenes.BuildStudioShowcase();
        await Assert.That(studio.Triangles.Length).IsGreaterThan(hello.Triangles.Length * 10);
    }
}
