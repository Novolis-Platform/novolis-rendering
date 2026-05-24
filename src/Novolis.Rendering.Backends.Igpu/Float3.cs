using System.Numerics;
using System.Runtime.CompilerServices;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>Plain float3 for ILGPU kernels (no System.Numerics.Vector3 intrinsics).</summary>
public struct Float3
{
    public float X;
    public float Y;
    public float Z;

    public Float3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Float3 Zero => new(0f, 0f, 0f);
    public static Float3 One => new(1f, 1f, 1f);
    public static Float3 UnitX => new(1f, 0f, 0f);
    public static Float3 UnitY => new(0f, 1f, 0f);

    public static Float3 From(Vector3 v) => new(v.X, v.Y, v.Z);

    public Vector3 ToVector3() => new(X, Y, Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Add(Float3 a, Float3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Sub(Float3 a, Float3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Scale(Float3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Mul(Float3 a, Float3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Cross(Float3 a, Float3 b) =>
        new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Length(Float3 v) => TracerMath.Sqrt(Dot(v, v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Normalize(Float3 v)
    {
        var len = Length(v);
        if (len < 1e-12f)
        {
            return UnitY;
        }

        var inv = 1f / len;
        return new Float3(v.X * inv, v.Y * inv, v.Z * inv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Reflect(Float3 direction, Float3 normal)
    {
        var d = Dot(direction, normal);
        return Sub(direction, Scale(normal, 2f * d));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Lerp(Float3 a, Float3 b, float t) =>
        new(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t);
}
