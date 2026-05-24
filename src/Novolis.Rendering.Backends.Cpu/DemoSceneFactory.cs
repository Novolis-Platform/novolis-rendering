using System.Numerics;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Scene;

namespace Novolis.Rendering.Backends.Cpu;

/// <summary>Builds a demo scene for tests and samples.</summary>
public static class DemoSceneFactory
{
    /// <summary>Builds a small room with ground, metal cube, and directional light.</summary>
    /// <returns>A compiled demo scene.</returns>
    public static CompiledScene UnitCubeRoom()
    {
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(new Vector3(0.35f, 0.37f, 0.4f), 0.9f))
            .AddBox(new Vector3(0f, 0.25f, 0f), new Vector3(0.25f, 0.25f, 0.25f), MaterialPresets.Metal(MaterialPresets.Colors.Silver, 0.08f))
            .AddDirectionalLight(new Vector3(-0.4f, -1f, -0.3f), Vector3.One)
            .Build();
        return SceneCompiler.Compile(scene);
    }
}
