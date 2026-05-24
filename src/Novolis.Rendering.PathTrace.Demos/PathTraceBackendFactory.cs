using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Backends.Vulkan;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>Creates <see cref="IRayTracingBackend"/> instances for demo apps.</summary>
public static class PathTraceBackendFactory
{
    /// <summary>Creates a backend for the given <paramref name="kind"/>.</summary>
    /// <param name="kind">Backend selector.</param>
    /// <returns>A new backend instance (caller owns disposal).</returns>
    public static IRayTracingBackend Create(PathTraceBackendKind kind) =>
        kind switch
        {
            PathTraceBackendKind.Cpu => new CpuRayTracingBackend(),
            PathTraceBackendKind.Vulkan => new VulkanRayTracingBackend(),
            _ => new IlgpuRayTracingBackend(),
        };

    /// <summary>Parses <c>NOVOLIS_RAY_BACKEND</c> (cpu, vulkan, default ilgpu).</summary>
    /// <returns>The resolved backend kind.</returns>
    public static PathTraceBackendKind FromEnvironment() =>
        Parse(Environment.GetEnvironmentVariable("NOVOLIS_RAY_BACKEND"));

    /// <summary>Parses a backend name (cpu, vulkan, ilgpu).</summary>
    /// <param name="name">Backend name or null for default.</param>
    /// <returns>The resolved backend kind.</returns>
    public static PathTraceBackendKind Parse(string? name)
    {
        if (string.Equals(name, "cpu", StringComparison.OrdinalIgnoreCase))
        {
            return PathTraceBackendKind.Cpu;
        }

        if (string.Equals(name, "vulkan", StringComparison.OrdinalIgnoreCase))
        {
            return PathTraceBackendKind.Vulkan;
        }

        return PathTraceBackendKind.Ilgpu;
    }
}
