using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

internal static class TwoDGridBlend
{
    public static void Over(ref Rgba32 dst, Rgba32 src)
    {
        if (src.A == 0)
        {
            return;
        }

        if (src.A == 255 || dst.A == 0)
        {
            dst = src;
            return;
        }

        var sa = src.A / 255f;
        var da = dst.A / 255f;
        var outA = sa + da * (1f - sa);
        if (outA <= 1e-6f)
        {
            dst = default;
            return;
        }

        byte Blend(byte s, byte d) => (byte)((s * sa + d * da * (1f - sa)) / outA);
        dst = new Rgba32(Blend(src.R, dst.R), Blend(src.G, dst.G), Blend(src.B, dst.B), (byte)(outA * 255f));
    }
}
