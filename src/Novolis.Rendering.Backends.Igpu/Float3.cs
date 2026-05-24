using System.Numerics;
using System.Runtime.CompilerServices;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>Plain float3 for ILGPU kernels (no System.Numerics.Vector3 intrinsics).</summary>
public struct Float3
{
    /// <summary>X component.</summary>
    public float X;

    /// <summary>Y component.</summary>
    public float Y;

    /// <summary>Z component.</summary>
    public float Z;

    /// <summary>Creates a vector from components.</summary>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    /// <param name="z">Z component.</param>
    public Float3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Zero vector.</summary>
    public static Float3 Zero => new(0f, 0f, 0f);

    /// <summary>Ones vector.</summary>
    public static Float3 One => new(1f, 1f, 1f);

    /// <summary>Unit X axis.</summary>
    public static Float3 UnitX => new(1f, 0f, 0f);

    /// <summary>Unit Y axis.</summary>
    public static Float3 UnitY => new(0f, 1f, 0f);

    /// <summary>Converts from <see cref="Vector3"/>.</summary>
    /// <param name="v">BCL vector.</param>
    /// <returns>Kernel-friendly float3.</returns>
    public static Float3 From(Vector3 v) => new(v.X, v.Y, v.Z);

    /// <summary>Converts to <see cref="Vector3"/>.</summary>
    /// <returns>BCL vector.</returns>
    public Vector3 ToVector3() => new(X, Y, Z);

    /// <summary>Component-wise addition.</summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <returns><c>a + b</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Add(Float3 a, Float3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>Component-wise subtraction.</summary>
    /// <param name="a">Minuend.</param>
    /// <param name="b">Subtrahend.</param>
    /// <returns><c>a - b</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Sub(Float3 a, Float3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>Scalar multiply.</summary>
    /// <param name="a">Vector.</param>
    /// <param name="s">Scalar.</param>
    /// <returns><c>a * s</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Scale(Float3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);

    /// <summary>Component-wise multiply.</summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <returns>Component-wise product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Mul(Float3 a, Float3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    /// <summary>Dot product.</summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <returns><c>dot(a, b)</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>Cross product.</summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <returns><c>cross(a, b)</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Cross(Float3 a, Float3 b) =>
        new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    /// <summary>Euclidean length.</summary>
    /// <param name="v">Vector.</param>
    /// <returns>Length of <paramref name="v"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Length(Float3 v) => TracerMath.Sqrt(Dot(v, v));

    /// <summary>Normalizes a vector; returns <see cref="UnitY"/> for near-zero input.</summary>
    /// <param name="v">Vector to normalize.</param>
    /// <returns>Unit vector in the direction of <paramref name="v"/>.</returns>
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

    /// <summary>Reflects a direction about a normal.</summary>
    /// <param name="direction">Incident direction.</param>
    /// <param name="normal">Surface normal.</param>
    /// <returns>Reflected direction.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Reflect(Float3 direction, Float3 normal)
    {
        var d = Dot(direction, normal);
        return Sub(direction, Scale(normal, 2f * d));
    }

    /// <summary>Linear interpolation.</summary>
    /// <param name="a">Start value.</param>
    /// <param name="b">End value.</param>
    /// <param name="t">Interpolation factor in [0, 1].</param>
    /// <returns><c>lerp(a, b, t)</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float3 Lerp(Float3 a, Float3 b, float t) =>
        new(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t);
}
