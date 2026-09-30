using System.Runtime.CompilerServices;
using ILGPU;
using ILGPU.Runtime;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>ILGPU-friendly math aliases.</summary>
internal static class TracerMath
{
    public static float PI => MathF.PI;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Abs(float v) => MathF.Abs(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(float a, float b) => MathF.Max(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(float a, float b) => MathF.Min(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sqrt(float v) => MathF.Sqrt(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cos(float v) => MathF.Cos(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sin(float v) => MathF.Sin(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
}
