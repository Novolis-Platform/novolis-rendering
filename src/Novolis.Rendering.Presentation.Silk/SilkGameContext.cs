using System.Numerics;
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
    private readonly HashSet<MouseButton> _mouseDownLastFrame = new();
    private readonly HashSet<MouseButton> _mousePolledThisFrame = new();
    private Vector2 _mousePosition;
    private Vector2 _previousMousePosition;
    private bool _mouseTracked;
    private Vector2 _mouseDelta;
    private float _scrollDelta;
    private int _width;
    private int _height;
    private float _dt;

    /// <summary>Current framebuffer width in pixels.</summary>
    public int Width => _width;

    /// <summary>Current framebuffer height in pixels.</summary>
    public int Height => _height;

    /// <summary>Elapsed time since the last frame in seconds.</summary>
    public float DeltaSeconds => _dt;

    /// <summary>Mouse position in window coordinates (first mouse).</summary>
    public Vector2 MousePosition => _mousePosition;

    /// <summary>Mouse movement since the previous frame.</summary>
    public Vector2 MouseDelta => _mouseDelta;

    /// <summary>Scroll wheel delta this frame (positive = zoom in).</summary>
    public float ScrollDelta => _scrollDelta;

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
        PollMouse();
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
    public bool IsResetPressed() => IsKeyPressed(Key.R);

    /// <summary>Returns whether the orbit toggle key (space) was pressed this frame.</summary>
    public bool IsOrbitTogglePressed() => IsKeyPressed(Key.Space);

    /// <summary>Returns whether a digit key (<c>1</c>–<c>3</c>) was pressed this frame.</summary>
    /// <param name="digit">Digit 1, 2, or 3.</param>
    /// <returns><see langword="true"/> on the transition to down.</returns>
    public bool IsDigitPressed(int digit)
    {
        if (digit is < 1 or > 3)
        {
            return false;
        }

        var key = digit switch
        {
            1 => Key.Number1,
            2 => Key.Number2,
            _ => Key.Number3,
        };
        return IsKeyPressed(key);
    }

    /// <summary>Returns whether <paramref name="button"/> is held down.</summary>
    public bool IsMouseButtonDown(MouseButton button)
    {
        _mousePolledThisFrame.Add(button);
        return AnyMouse(m => m.IsButtonPressed(button));
    }

    /// <summary>True only on the frame the mouse button transitioned to down.</summary>
    public bool IsMouseButtonPressed(MouseButton button) =>
        IsMouseButtonDown(button) && !_mouseDownLastFrame.Contains(button);

    /// <summary>Returns whether the backend cycle key (<c>B</c>) was pressed this frame.</summary>
    public bool IsBackendCyclePressed() => IsKeyPressed(Key.B);

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

        _mouseDownLastFrame.Clear();
        foreach (var mouse in _input?.Mice ?? [])
        {
            foreach (var button in _mousePolledThisFrame)
            {
                if (mouse.IsButtonPressed(button))
                {
                    _mouseDownLastFrame.Add(button);
                }
            }
        }

        _mousePolledThisFrame.Clear();
    }

    private void PollMouse()
    {
        _mouseDelta = Vector2.Zero;
        _scrollDelta = 0f;
        foreach (var mouse in _input?.Mice ?? [])
        {
            var pos = mouse.Position;
            if (_mouseTracked)
            {
                _mouseDelta += pos - _previousMousePosition;
            }

            _previousMousePosition = pos;
            _mousePosition = pos;
            _mouseTracked = true;
            foreach (var scroll in mouse.ScrollWheels)
            {
                _scrollDelta += scroll.Y;
            }
        }
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

    private bool AnyMouse(Func<IMouse, bool> predicate)
    {
        foreach (var mouse in _input?.Mice ?? [])
        {
            if (predicate(mouse))
            {
                return true;
            }
        }

        return false;
    }
}
