using Microsoft.Extensions.DependencyInjection;
using Novolis.Rendering.DependencyInjection;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Scene;

namespace Novolis.Rendering.DependencyInjection.Tests;

public sealed class RenderingDiExtendedTests
{
    [Test]
    public async Task AddRayTracing_registers_scene_compiler_service()
    {
        var services = new ServiceCollection();
        services.AddRayTracing();
        var provider = services.BuildServiceProvider();

        var compiler = provider.GetRequiredService<SceneCompilerService>();
        var scene = new SceneBuilder()
            .AddGround(MaterialPresets.Standard(MaterialPresets.Colors.Silver))
            .Build();

        var compiled = compiler.Compile(scene);

        await Assert.That(compiled.Triangles.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task AddRayTracingFromEnvironment_cpu_backend_when_env_set()
    {
        var previous = Environment.GetEnvironmentVariable("NOVOLIS_RAY_BACKEND");
        try
        {
            Environment.SetEnvironmentVariable("NOVOLIS_RAY_BACKEND", "cpu");
            var services = new ServiceCollection();
            services.AddRayTracingFromEnvironment();
            var provider = services.BuildServiceProvider();

            var backend = provider.GetRequiredService<IRayTracingBackend>();
            await Assert.That(backend.GetType().Name).Contains("Cpu");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NOVOLIS_RAY_BACKEND", previous);
        }
    }

    [Test]
    public async Task UseCpuBackend_and_UseIlgpuBackend_register_backends()
    {
        var cpuServices = new ServiceCollection();
        cpuServices.AddRayTracing().UseCpuBackend(deterministic: true);
        await Assert.That(cpuServices.BuildServiceProvider().GetRequiredService<IRayTracingBackend>()).IsNotNull();

        var igpuServices = new ServiceCollection();
        igpuServices.AddRayTracing().UseIlgpuBackend();
        await Assert.That(igpuServices.BuildServiceProvider().GetRequiredService<IRayTracingBackend>()).IsNotNull();
    }
}
