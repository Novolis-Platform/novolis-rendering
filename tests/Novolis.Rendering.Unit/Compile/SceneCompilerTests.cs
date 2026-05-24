using System.Numerics;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;
using TUnit.Core;

namespace Novolis.Rendering.Compile.Tests;

public sealed class SceneCompilerTests
{
    [Test]
    public async Task Compile_UnitCubeRoom_HasTrianglesAndBvh()
    {
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(Vector3.One, 0.8f))
            .AddBox(Vector3.Zero, new Vector3(0.25f), MaterialPresets.Metal(MaterialPresets.Colors.Silver))
            .AddDirectionalLight(new Vector3(0, -1, 0), Vector3.One)
            .Build();

        var compiled = SceneCompiler.Compile(scene);
        await Assert.That(compiled.Triangles.Length).IsGreaterThan(0);
        await Assert.That(compiled.Materials.Length).IsEqualTo(2);
        await Assert.That(compiled.BvhRootIndex).IsGreaterThanOrEqualTo(0);
    }
}
