using Novolis.Rendering.PathTrace.Demos;
using TUnit.Core;

namespace Novolis.Rendering.PathTrace.Demos.Tests;

public sealed class PathTraceBackendFactoryTests
{
    [Test]
    public async Task Parse_cpu_returns_cpu()
    {
        await Assert.That(PathTraceBackendFactory.Parse("cpu")).IsEqualTo(PathTraceBackendKind.Cpu);
    }

    [Test]
    public async Task Parse_vulkan_returns_vulkan()
    {
        await Assert.That(PathTraceBackendFactory.Parse("vulkan")).IsEqualTo(PathTraceBackendKind.Vulkan);
    }

    [Test]
    public async Task Parse_null_returns_ilgpu()
    {
        await Assert.That(PathTraceBackendFactory.Parse(null)).IsEqualTo(PathTraceBackendKind.Ilgpu);
    }
}
