using System.Numerics;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Runtime;
using TUnit.Core;

namespace Novolis.Rendering.Materials.Tests;

public sealed class MaterialCompilerTests
{
    [Test]
    public async Task Metal_Preset_CompilesToStandardModel()
    {
        var gpu = MaterialCompiler.Compile(Materials.Metal(Materials.Colors.Silver, 0.08f));
        await Assert.That(gpu.Model).IsEqualTo(MaterialModel.Standard);
        await Assert.That(gpu.B.X).IsEqualTo(1f);
        await Assert.That(gpu.A.W).IsEqualTo(0.08f);
    }

    [Test]
    public async Task Glass_Compile_PacksIorInB()
    {
        var gpu = MaterialCompiler.Compile(Materials.Glass(Vector3.One, ior: 1.52f));
        await Assert.That(gpu.Model).IsEqualTo(MaterialModel.Glass);
        await Assert.That(gpu.B.X).IsEqualTo(1.52f);
    }
}
