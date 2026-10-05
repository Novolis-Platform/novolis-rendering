namespace Novolis.Rendering.Appearance;

/// <summary>Deterministic 2D value noise. Frequency is the caller's Scale.</summary>
public static class AppearanceNoise
{
    /// <summary>Returns [0,1] value noise at (x, y).</summary>
    public static float Value(float x, float y, int seed)
    {
        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);
        var fx = x - x0;
        var fy = y - y0;
        var a = Hash(x0, y0, seed);
        var b = Hash(x0 + 1, y0, seed);
        var c = Hash(x0, y0 + 1, seed);
        var d = Hash(x0 + 1, y0 + 1, seed);
        var u = fx * fx * (3f - 2f * fx);
        var v = fy * fy * (3f - 2f * fy);
        return (a + (b - a) * u) + ((c + (d - c) * u) - (a + (b - a) * u)) * v;
    }

    private static float Hash(int x, int y, int seed)
    {
        var n = x * 374761393 + y * 668265263 + seed * 1274126177;
        n = (n ^ (n >> 13)) * 1274126177;
        n ^= n >> 16;
        return (n & 0x7fffffff) / 2147483647f;
    }
}
