using System.Numerics;

namespace Novolis.Rendering.Runtime;

public enum MaterialModel
{
    Standard,
    Glass,
    Skin,
    Emissive,
}

/// <summary>Fixed-size GPU/runtime material (blittable).</summary>
public readonly record struct GpuMaterial
{
    public GpuMaterial(MaterialModel model, Vector4 a, Vector4 b, Vector4 c)
    {
        Model = model;
        A = a;
        B = b;
        C = c;
    }

    public MaterialModel Model { get; }
    public Vector4 A { get; }
    public Vector4 B { get; }
    public Vector4 C { get; }
}
