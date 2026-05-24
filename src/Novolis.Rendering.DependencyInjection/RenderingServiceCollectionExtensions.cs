using Microsoft.Extensions.DependencyInjection;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Backends.Vulkan;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.DependencyInjection;

/// <summary>Registers rendering services and ray tracing backends with DI.</summary>
public static class RenderingServiceCollectionExtensions
{
    /// <summary>Adds scene compilation services (no backend selected).</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddRayTracing(this IServiceCollection services)
    {
        services.AddSingleton<SceneCompilerService>();
        return services;
    }

    /// <summary>Registers the CPU path tracing backend.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="deterministic">When true, uses a deterministic RNG for tests.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection UseCpuBackend(this IServiceCollection services, bool deterministic = false)
    {
        services.AddSingleton<IRayTracingBackend>(_ => new CpuRayTracingBackend(deterministic));
        return services;
    }

    /// <summary>Registers the ILGPU compute backend.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection UseIlgpuBackend(this IServiceCollection services)
    {
        services.AddSingleton<IlgpuRayTracingBackend>();
        services.AddSingleton<IRayTracingBackend>(sp => sp.GetRequiredService<IlgpuRayTracingBackend>());
        return services;
    }

    /// <summary>Registers the Vulkan compute backend.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection UseVulkanBackend(this IServiceCollection services)
    {
        services.AddSingleton<VulkanRayTracingBackend>();
        services.AddSingleton<IRayTracingBackend>(sp => sp.GetRequiredService<VulkanRayTracingBackend>());
        return services;
    }

    /// <summary>
    /// Adds ray tracing and selects a backend from <c>NOVOLIS_RAY_BACKEND</c>
    /// (<c>cpu</c>, <c>vulkan</c>, default ILGPU).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddRayTracingFromEnvironment(this IServiceCollection services)
    {
        services.AddRayTracing();
        var backend = Environment.GetEnvironmentVariable("NOVOLIS_RAY_BACKEND");
        if (string.Equals(backend, "cpu", StringComparison.OrdinalIgnoreCase))
        {
            return services.UseCpuBackend();
        }

        if (string.Equals(backend, "vulkan", StringComparison.OrdinalIgnoreCase))
        {
            return services.UseVulkanBackend();
        }

        return services.UseIlgpuBackend();
    }
}

/// <summary>DI-friendly wrapper around static <see cref="SceneCompiler"/>.</summary>
public sealed class SceneCompilerService
{
    /// <summary>Compiles an authoring scene into a <see cref="CompiledScene"/>.</summary>
    /// <param name="scene">Authoring scene.</param>
    /// <returns>Flat runtime scene.</returns>
    public CompiledScene Compile(Novolis.Rendering.Scene.Scene scene) => SceneCompiler.Compile(scene);
}
