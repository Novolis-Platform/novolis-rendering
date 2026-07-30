using SilkKey = Silk.NET.Input.Key;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Novolis.Rendering.Backends.TwoD.Silk;

internal static class SilkInputMapping
{
    public static SilkKey ToSilk(Novolis.Rendering.Presentation.Key key) => (SilkKey)(int)key;

    public static SilkMouseButton ToSilk(Novolis.Rendering.Presentation.MouseButton button) =>
        (SilkMouseButton)(int)button;
}
