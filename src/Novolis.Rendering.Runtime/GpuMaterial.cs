using System.Numerics;

namespace Novolis.Rendering.Runtime;

/// <summary>Fixed-size GPU/runtime material (blittable).</summary>
public readonly record struct GpuMaterial
{
    /// <summary>Packs model-specific parameters into three <see cref="Vector4"/> slots.</summary>
    /// <param name="model">Shading model.</param>
    /// <param name="a">First parameter block.</param>
    /// <param name="b">Second parameter block.</param>
    /// <param name="c">Third parameter block.</param>
    public GpuMaterial(MaterialModel model, Vector4 a, Vector4 b, Vector4 c)
    {
        Model = model;
        A = a;
        B = b;
        C = c;
    }

    /// <summary>Active shading model.</summary>
    public MaterialModel Model { get; }

    /// <summary>Model-specific parameters (layout depends on <see cref="Model"/>).</summary>
    public Vector4 A { get; }

    /// <summary>Model-specific parameters (layout depends on <see cref="Model"/>).</summary>
    public Vector4 B { get; }

    /// <summary>Model-specific parameters (layout depends on <see cref="Model"/>).</summary>
    public Vector4 C { get; }
}
