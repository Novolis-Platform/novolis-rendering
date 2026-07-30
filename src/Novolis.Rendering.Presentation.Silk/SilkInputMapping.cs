using SilkKey = Silk.NET.Input.Key;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Novolis.Rendering.Presentation.Silk;

internal static class SilkInputMapping
{
    public static SilkKey ToSilk(Key key) => (SilkKey)(int)key;

    public static SilkMouseButton ToSilk(MouseButton button) => (SilkMouseButton)(int)button;
}
