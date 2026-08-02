using System.Numerics;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Materials.Tests;

public sealed class SkinMaterialTests
{
    [Test]
    public async Task SkinMaterial_compiles_to_skin_gpu_model()
    {
        var skin = new SkinMaterial
        {
            BaseColor = new Vector3(0.9f, 0.7f, 0.6f),
            Roughness = 0.4f,
            SubsurfaceStrength = 0.6f,
            BloodTint = new Vector3(0.7f, 0.1f, 0.05f),
            ScatterRadius = 0.03f,
        };

        var gpu = MaterialCompiler.Compile(skin);

        await Assert.That(gpu.Model).IsEqualTo(MaterialModel.Skin);
        await Assert.That(gpu.A.X).IsEqualTo(0.9f);
        await Assert.That(gpu.B.X).IsEqualTo(0.6f);
        await Assert.That(gpu.C.X).IsEqualTo(0.7f);
    }

    [Test]
    public async Task EmissiveMaterial_compiles_to_emissive_model()
    {
        var emissive = new EmissiveMaterial
        {
            EmissionColor = new Vector3(1f, 0.2f, 0.1f),
            EmissionStrength = 2.5f,
        };

        var gpu = MaterialCompiler.Compile(emissive);

        await Assert.That(gpu.Model).IsEqualTo(MaterialModel.Emissive);
        await Assert.That(gpu.A.W).IsEqualTo(2.5f);
    }

    [Test]
    public async Task MaterialCompiler_rejects_unknown_material_type()
    {
        await Assert.That(() => MaterialCompiler.Compile(new UnknownMaterial()))
            .ThrowsExactly<ArgumentException>();
    }

    sealed class UnknownMaterial : IMaterial;
}
