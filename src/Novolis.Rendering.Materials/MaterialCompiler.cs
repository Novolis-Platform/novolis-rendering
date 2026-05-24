using System.Numerics;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Materials;

/// <summary>Compiles authoring materials into flat <see cref="GpuMaterial"/> records.</summary>
public static class MaterialCompiler
{
    /// <summary>Compiles any supported <see cref="IMaterial"/> into a <see cref="GpuMaterial"/>.</summary>
    /// <param name="material">Authoring material instance.</param>
    /// <returns>Blittable GPU material.</returns>
    /// <exception cref="ArgumentException">Thrown for unknown material types.</exception>
    public static GpuMaterial Compile(IMaterial material) => material switch
    {
        StandardMaterial s => CompileStandard(s),
        GlassMaterial g => CompileGlass(g),
        SkinMaterial sk => CompileSkin(sk),
        EmissiveMaterial e => CompileEmissive(e),
        _ => throw new ArgumentException($"Unknown material type: {material.GetType().Name}", nameof(material)),
    };

    private static GpuMaterial CompileStandard(StandardMaterial s)
    {
        var a = new Vector4(s.BaseColor, s.Roughness);
        var b = new Vector4(s.Metallic, s.EmissionStrength, 0f, 0f);
        var c = new Vector4(s.EmissionColor, 0f);
        return new GpuMaterial(MaterialModel.Standard, a, b, c);
    }

    private static GpuMaterial CompileGlass(GlassMaterial g)
    {
        var a = new Vector4(g.Tint, g.Roughness);
        var b = new Vector4(g.Ior, g.Absorption, 0f, 0f);
        return new GpuMaterial(MaterialModel.Glass, a, b, default);
    }

    private static GpuMaterial CompileSkin(SkinMaterial s)
    {
        var a = new Vector4(s.BaseColor, s.Roughness);
        var b = new Vector4(s.SubsurfaceStrength, s.ScatterRadius, 0f, 0f);
        var c = new Vector4(s.BloodTint, 0f);
        return new GpuMaterial(MaterialModel.Skin, a, b, c);
    }

    private static GpuMaterial CompileEmissive(EmissiveMaterial e)
    {
        var a = new Vector4(e.EmissionColor, e.EmissionStrength);
        return new GpuMaterial(MaterialModel.Emissive, a, default, default);
    }
}
