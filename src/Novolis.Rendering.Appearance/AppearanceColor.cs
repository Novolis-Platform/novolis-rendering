using Novolis.Math.Geometry;

namespace Novolis.Rendering.Appearance;

/// <summary>Byte-color helpers for appearance evaluation.</summary>
internal static class AppearanceColor
{
    public static Rgba32 Grey => new(158, 158, 162);

    public static Rgba32 Lerp(Rgba32 a, Rgba32 b, float t)
    {
        t = System.Math.Clamp(t, 0f, 1f);
        return new Rgba32(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t));
    }

    public static Rgba32 Multiply(Rgba32 a, Rgba32 b) =>
        new(
            (byte)(a.R * b.R / 255),
            (byte)(a.G * b.G / 255),
            (byte)(a.B * b.B / 255),
            (byte)(a.A * b.A / 255));

    public static Rgba32 Scale(Rgba32 c, float s) =>
        new(Mul(c.R, s), Mul(c.G, s), Mul(c.B, s), c.A);

    public static Rgba32 Add(Rgba32 a, Rgba32 b) =>
        new(
            (byte)System.Math.Clamp(a.R + b.R, 0, 255),
            (byte)System.Math.Clamp(a.G + b.G, 0, 255),
            (byte)System.Math.Clamp(a.B + b.B, 0, 255),
            (byte)System.Math.Clamp(a.A + b.A, 0, 255));

    public static Rgba32 WithAlpha(Rgba32 c, float alpha) =>
        new(c.R, c.G, c.B, (byte)System.Math.Clamp((int)(alpha * 255f), 0, 255));

    public static float Luma(Rgba32 c) => (c.R * 0.2126f + c.G * 0.7152f + c.B * 0.0722f) / 255f;

    private static byte Mul(byte c, float s) => (byte)System.Math.Clamp((int)(c * s), 0, 255);
}
