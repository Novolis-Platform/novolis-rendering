using Microsoft.Extensions.DependencyInjection;
using Novolis.Rendering.DependencyInjection;
using Novolis.Rendering.Runtime;
using TUnit.Core;

namespace Novolis.Rendering.Unit;

public sealed class RenderingMetaPackageTests
{
    [Test]
    public async Task MetaPackage_WiresRayTracingDi()
    {
        var services = new ServiceCollection();
        services.AddRayTracing().UseCpuBackend(deterministic: true);
        var backend = services.BuildServiceProvider().GetRequiredService<IRayTracingBackend>();
        await Assert.That(backend).IsNotNull();
    }
}
