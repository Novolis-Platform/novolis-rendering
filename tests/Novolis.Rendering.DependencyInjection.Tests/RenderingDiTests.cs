using Microsoft.Extensions.DependencyInjection;
using Novolis.Rendering.DependencyInjection;
using Novolis.Rendering.Presentation.Abstractions;
using Novolis.Rendering.Runtime;
using TUnit.Core;

namespace Novolis.Rendering.DependencyInjection.Tests;

public sealed class RenderingDiTests
{
    [Test]
    public async Task AddRayTracing_RegistersBackend()
    {
        var services = new ServiceCollection();
        services.AddRayTracing().UseCpuBackend(deterministic: true);
        var provider = services.BuildServiceProvider();
        var backend = provider.GetRequiredService<IRayTracingBackend>();
        await Assert.That(backend).IsNotNull();
    }
}
