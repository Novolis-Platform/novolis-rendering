using System.Numerics;
using Novolis.Rendering.Materials;

namespace Novolis.Rendering.Materials.Tests;

public sealed class MaterialPresetsTests
{
    [Test]
    public async Task Colors_presets_are_non_zero()
    {
        await Assert.That(MaterialPresets.Colors.Silver.X).IsGreaterThan(0.5f);
        await Assert.That(MaterialPresets.Colors.White).IsEqualTo(Vector3.One);
        await Assert.That(MaterialPresets.Colors.Red.X).IsGreaterThan(0.5f);
    }

    [Test]
    public async Task Skin_and_Emissive_presets_compile()
    {
        var skin = MaterialPresets.Skin(new Vector3(0.8f, 0.6f, 0.5f));
        var emissive = MaterialPresets.Emissive(Vector3.UnitX, strength: 3f);

        await Assert.That(skin.Roughness).IsEqualTo(0.45f);
        await Assert.That(emissive.EmissionStrength).IsEqualTo(3f);
    }
}
