using Microsoft.Extensions.DependencyInjection;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Backends.Vulkan;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.DependencyInjection;

/// <summary>DI-friendly wrapper around static <see cref="SceneCompiler"/>.</summary>
public sealed class SceneCompilerService
{
    /// <summary>Compiles an authoring scene into a <see cref="CompiledScene"/>.</summary>
    /// <param name="scene">Authoring scene.</param>
    /// <returns>Flat runtime scene.</returns>
    public CompiledScene Compile(Novolis.Rendering.Scene.Scene scene) => SceneCompiler.Compile(scene);
}
