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
    private readonly HashSet<Key> _keysPolledThisFrame = new();
    private int _width;
    private int _height;
    private float _dt;

    /// <summary>Current framebuffer width in pixels.</summary>
    public int Width => _width;

    /// <summary>Current framebuffer height in pixels.</summary>
    public int Height => _height;

    /// <summary>Elapsed time since the last frame in seconds.</summary>
    public float DeltaSeconds => _dt;

    /// <summary>OpenGL presenter bound to the window.</summary>
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

    /// <summary>Returns whether <paramref name="key"/> is held down.</summary>
    /// <param name="key">Silk key code.</param>
    /// <returns><see langword="true"/> when any keyboard reports the key pressed.</returns>
    public bool IsKeyDown(Key key)
    {
        if (!IsSupportedKey(key))
        {
            return false;
        }

        _keysPolledThisFrame.Add(key);
        return AnyKeyboard(k => k.IsKeyPressed(key));
    }

    /// <summary>True only on the frame the key transitioned to down (Raylib-style pressed).</summary>
    public bool IsKeyPressed(Key key) => IsKeyDown(key) && !_keysDownLastFrame.Contains(key);

    /// <summary>Returns whether the reset key (<c>R</c>) was pressed this frame.</summary>
    /// <returns><see langword="true"/> on the transition to down.</returns>
    public bool IsResetPressed() => IsKeyPressed(Key.R);

    /// <summary>Returns whether the orbit toggle key (space) was pressed this frame.</summary>
    /// <returns><see langword="true"/> on the transition to down.</returns>
    public bool IsOrbitTogglePressed() => IsKeyPressed(Key.Space);

    internal void EndInputFrame()
    {
        _keysDownLastFrame.Clear();
        foreach (var keyboard in _input?.Keyboards ?? [])
        {
            foreach (var key in _keysPolledThisFrame)
            {
                if (keyboard.IsKeyPressed(key))
                {
                    _keysDownLastFrame.Add(key);
                }
            }
        }

        _keysPolledThisFrame.Clear();
    }

    private static bool IsSupportedKey(Key key) =>
        key != Key.Unknown && (int)key >= 0;

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
