using System.Numerics;

namespace Novolis.Rendering.Runtime;

/// <summary>World-space triangle with material index.</summary>
public readonly record struct GpuTriangle(Vector4 A, Vector4 B, Vector4 C, int MaterialIndex);
