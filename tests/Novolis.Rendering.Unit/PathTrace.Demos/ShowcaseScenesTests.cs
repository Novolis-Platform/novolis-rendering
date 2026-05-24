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
    public async Task BuildStudioShowcase_has_more_lights_than_hello()
    {
        var hello = ShowcaseScenes.BuildHelloShowcase();
        var studio = ShowcaseScenes.BuildStudioShowcase();
        await Assert.That(studio.Lights.Length).IsGreaterThan(hello.Lights.Length);
    }
}
