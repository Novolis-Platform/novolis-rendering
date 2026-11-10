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
    public async Task BuildStudioShowcase_extracts_emissive_nee_lights()
    {
        var studio = ShowcaseScenes.BuildStudioShowcase();
        var neeLights = 0;
        var hasRect = false;
        foreach (var light in studio.Lights)
        {
            if (light.Kind is Novolis.Rendering.Runtime.GpuLightKind.Point
                or Novolis.Rendering.Runtime.GpuLightKind.Rect)
            {
                neeLights++;
            }

            if (light.Kind == Novolis.Rendering.Runtime.GpuLightKind.Rect)
            {
                hasRect = true;
            }
        }

        await Assert.That(neeLights).IsGreaterThanOrEqualTo(1);
        await Assert.That(hasRect).IsTrue();
    }

    [Test]
    public async Task BuildStudioShowcase_is_a_dense_diorama()
    {
        var hello = ShowcaseScenes.BuildHelloShowcase();
        var studio = ShowcaseScenes.BuildStudioShowcase();
        await Assert.That(studio.Triangles.Length).IsGreaterThan(hello.Triangles.Length * 10);
    }
}
