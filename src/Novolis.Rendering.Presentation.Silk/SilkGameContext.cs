using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Novolis.Rendering.Presentation.Silk;

/// <summary>Per-frame state and input for the Silk game loop.</summary>
public sealed class SilkGameContext
{
    private IWindow? _window;
    private IInputContext? _input;
    private SilkOpenGlFramePresenter? _presenter;
    private readonly HashSet<Key> _keysDownLastFrame = new();
    private int _width;
    private int _height;
    private float _dt;

    public int Width => _width;

    public int Height => _height;

    public float DeltaSeconds => _dt;

    public SilkOpenGlFramePresenter FramePresenter =>
        _presenter ?? throw new InvalidOperationException("Silk presenter is not bound.");

    internal void Bind(IWindow window, SilkOpenGlFramePresenter presenter, IInputContext input)
    {
        _window = window;
        _presenter = presenter;
        _input = input;
    }

    internal void SetFrame(Vector2D<int> size, float deltaSeconds)
    {
        _width = size.X;
        _height = size.Y;
        _dt = deltaSeconds;
    }

    /// <summary>Updates the window title (used as a lightweight HUD).</summary>
    public void SetTitle(string title)
    {
        if (_window is not null)
        {
            _window.Title = title;
        }
    }

    public bool IsKeyDown(Key key) => AnyKeyboard(k => k.IsKeyPressed(key));

    /// <summary>True only on the frame the key transitioned to down (Raylib-style pressed).</summary>
    public bool IsKeyPressed(Key key) => IsKeyDown(key) && !_keysDownLastFrame.Contains(key);

    public bool IsResetPressed() => IsKeyPressed(Key.R);

    public bool IsOrbitTogglePressed() => IsKeyPressed(Key.Space);

    internal void EndInputFrame()
    {
        _keysDownLastFrame.Clear();
        foreach (var keyboard in _input?.Keyboards ?? [])
        {
            foreach (Key key in Enum.GetValues<Key>())
            {
                if (keyboard.IsKeyPressed(key))
                {
                    _keysDownLastFrame.Add(key);
                }
            }
        }
    }

    private bool AnyKeyboard(Func<IKeyboard, bool> predicate)
    {
        foreach (var keyboard in _input?.Keyboards ?? [])
        {
            if (predicate(keyboard))
            {
                return true;
            }
        }

        return false;
    }
}
