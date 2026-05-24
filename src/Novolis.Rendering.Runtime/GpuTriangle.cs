using System.Numerics;

namespace Novolis.Rendering.Runtime;

/// <summary>World-space triangle with material index.</summary>
/// <param name="A">First corner (xyz in <c>.X/.Y/.Z</c>).</param>
/// <param name="B">Second corner.</param>
/// <param name="C">Third corner.</param>
/// <param name="MaterialIndex">Index into the compiled material table.</param>
public readonly record struct GpuTriangle(Vector4 A, Vector4 B, Vector4 C, int MaterialIndex);
