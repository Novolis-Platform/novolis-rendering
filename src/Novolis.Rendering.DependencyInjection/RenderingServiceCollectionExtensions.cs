using Microsoft.Extensions.DependencyInjection;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Backends.Vulkan;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.DependencyInjection;

public static class RenderingServiceCollectionExtensions
{
    public static IServiceCollection AddRayTracing(this IServiceCollection services)
    {
        services.AddSingleton<SceneCompilerService>();
        return services;
    }

    public static IServiceCollection UseCpuBackend(this IServiceCollection services, bool deterministic = false)
    {
        services.AddSingleton<IRayTracingBackend>(_ => new CpuRayTracingBackend(deterministic));
        return services;
    }

    public static IServiceCollection UseIlgpuBackend(this IServiceCollection services)
    {
        services.AddSingleton<IRayTracingBackend, IlgpuRayTracingBackend>();
        return services;
    }

    public static IServiceCollection UseVulkanBackend(this IServiceCollection services)
    {
        services.AddSingleton<IRayTracingBackend, VulkanRayTracingBackend>();
        return services;
    }
}

/// <summary>DI-friendly wrapper around static <see cref="SceneCompiler"/>.</summary>
public sealed class SceneCompilerService
{
    public CompiledScene Compile(Novolis.Rendering.Scene.Scene scene) => SceneCompiler.Compile(scene);
}
