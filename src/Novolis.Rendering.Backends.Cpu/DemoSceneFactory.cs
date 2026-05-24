using System.Numerics;
using Novolis.Rendering.Abstractions;
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

/// <summary>Legacy bootstrap tracer; prefer <see cref="CpuRayTracingBackend"/>.</summary>
#pragma warning disable CS0618
[Obsolete("Use CpuRayTracingBackend with CompiledScene.")]
public sealed class CpuRayTracer : IRayTracer
{
    private readonly CpuRayTracingBackend _backend = new(deterministic: true);
    private CompiledScene _scene = CompiledScene.Empty;

    /// <inheritdoc />
    public void Render(ImageBuffer target, in RenderCamera camera, RenderScene scene)
    {
        _scene = BootstrapSceneAdapter.CompileLegacy(scene);
        _backend.ResizeAsync(target.Width, target.Height).GetAwaiter().GetResult();
        _backend.UploadSceneAsync(_scene).GetAwaiter().GetResult();
        var snapshot = CameraSnapshot.LookAt(
            camera.Position,
            camera.Position + camera.Forward,
            camera.Up,
            camera.VerticalFovRadians * (180f / MathF.PI),
            camera.AspectRatio);
        _backend.RenderAsync(snapshot, 0).GetAwaiter().GetResult();
        if (_backend.Output.TryGetCpuPixels(out var src, out var w, out var h) && w == target.Width && h == target.Height)
        {
            src.CopyTo(target.AsSpan());
        }
    }
}

internal static class BootstrapSceneAdapter
{
    public static CompiledScene CompileLegacy(RenderScene legacy)
    {
        var builder = new SceneBuilder();
        foreach (var mesh in legacy.Meshes)
        {
            builder.AddMesh(
                mesh.Vertices,
                mesh.TriangleIndices,
                MaterialPresets.Standard(
                    new Vector3(mesh.Color.R / 255f, mesh.Color.G / 255f, mesh.Color.B / 255f),
                    0.6f));
        }

        builder.AddDirectionalLight(new Vector3(-0.4f, -1f, -0.3f), Vector3.One);
        return SceneCompiler.Compile(builder.Build());
    }
}
#pragma warning restore CS0618
