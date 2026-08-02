using System.Numerics;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;
using TUnit.Core;

namespace Novolis.Rendering.Scene.Tests;

public sealed class SceneBuilderTests
{
    [Test]
    public async Task Build_AccumulatesMeshesAndLights()
    {
        var material = MaterialPresets.Standard(MaterialPresets.Colors.Red);
        var scene = new SceneBuilder()
            .AddGround(material, size: 4f)
            .AddBox(Vector3.Zero, new Vector3(0.5f), material)
            .AddDirectionalLight(new Vector3(0f, -1f, 0f), Vector3.One, intensity: 2f)
            .Build();

        await Assert.That(scene.Meshes.Count).IsEqualTo(2);
        await Assert.That(scene.Lights.Count).IsEqualTo(1);
        await Assert.That(scene.Lights[0].Kind).IsEqualTo(LightKind.Directional);
        await Assert.That(scene.Lights[0].Intensity).IsEqualTo(2f);
    }

    [Test]
    public async Task AddGround_CreatesHorizontalQuad()
    {
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(Vector3.One))
            .Build();

        await Assert.That(scene.Meshes.Count).IsEqualTo(1);
        await Assert.That(scene.Meshes[0].Vertices.Length).IsEqualTo(4);
        await Assert.That(scene.Meshes[0].TriangleIndices.Length).IsEqualTo(6);
    }
}
