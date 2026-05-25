using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Backends.TwoD.Silk;

/// <summary>Per-frame state for the Silk 2D game loop.</summary>
public sealed class SilkTwoDGameContext
{
    private IWindow? _window;
    private IInputContext? _input;
    private SilkTwoDRenderer? _renderer;
    private readonly HashSet<Key> _keysDownLastFrame = new();
    private readonly HashSet<Key> _keysPolledThisFrame = new();
    private readonly HashSet<MouseButton> _mouseDownLastFrame = new();
    private readonly HashSet<MouseButton> _mousePolledThisFrame = new();
    private Vector2D<float> _mousePosition;
    private Vector2D<float> _mouseDelta;
    private int _width;
    private int _height;
    private float _dt;

    /// <summary>Scene owned by the running game (textures, sprites, collision, HUD, menus).</summary>
    public TwoDScene Scene { get; } = new();

    /// <summary>OpenGL renderer bound to the window.</summary>
    public SilkTwoDRenderer Renderer =>
        _renderer ?? throw new InvalidOperationException("Renderer is not bound.");

    /// <summary>Framebuffer width in pixels.</summary>
    public int Width => _width;

    /// <summary>Framebuffer height in pixels.</summary>
    public int Height => _height;

    /// <summary>Elapsed time since the last frame in seconds.</summary>
    public float DeltaSeconds => _dt;

    /// <summary>Cursor position in framebuffer pixels (origin top-left).</summary>
    public Vector2D<float> MousePosition => _mousePosition;

    /// <summary>Cursor delta since the previous frame in pixels.</summary>
    public Vector2D<float> MouseDelta => _mouseDelta;

    internal void Bind(IWindow window, SilkTwoDRenderer renderer, IInputContext input)
    {
        _window = window;
        _renderer = renderer;
        _input = input;
    }

    internal void SetFrame(Vector2D<int> size, float deltaSeconds)
    {
        _width = size.X;
        _height = size.Y;
        _dt = deltaSeconds;
        _renderer?.Resize(_width, _height);
        PollMouse();
    }

    /// <summary>Whether <paramref name="button"/> is held.</summary>
    public bool IsMouseButtonDown(MouseButton button)
    {
        _mousePolledThisFrame.Add(button);
        return AnyMouse(m => m.IsButtonPressed(button));
    }

    /// <summary>True on the frame the button transitioned to down.</summary>
    public bool IsMouseButtonPressed(MouseButton button) =>
        IsMouseButtonDown(button) && !_mouseDownLastFrame.Contains(button);

    /// <summary>Updates the window title.</summary>
    public void SetTitle(string title)
    {
        if (_window is not null)
        {
            _window.Title = title;
        }
    }

    /// <summary>Whether <paramref name="key"/> is held.</summary>
    public bool IsKeyDown(Key key)
    {
        if (key == Key.Unknown)
        {
            return false;
        }

        _keysPolledThisFrame.Add(key);
        return AnyKeyboard(k => k.IsKeyPressed(key));
    }

    /// <summary>True on the frame the key transitioned to down.</summary>
    public bool IsKeyPressed(Key key) => IsKeyDown(key) && !_keysDownLastFrame.Contains(key);

    /// <summary>Menu navigation: up on W or arrow up pressed this frame.</summary>
    public bool IsMenuUpPressed() =>
        IsKeyPressed(Key.W) || IsKeyPressed(Key.Up);

    /// <summary>Menu navigation: down on S or arrow down pressed this frame.</summary>
    public bool IsMenuDownPressed() =>
        IsKeyPressed(Key.S) || IsKeyPressed(Key.Down);

    /// <summary>Confirm menu selection (Enter or Space).</summary>
    public bool IsMenuConfirmPressed() =>
        IsKeyPressed(Key.Enter) || IsKeyPressed(Key.Space);

    /// <summary>Cancel / back (Escape).</summary>
    public bool IsMenuCancelPressed() => IsKeyPressed(Key.Escape);

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
        _mouseDelta = default;
        foreach (var mouse in _input?.Mice ?? [])
        {
            var p = mouse.Position;
            var pos = new Vector2D<float>(p.X, p.Y);
            _mouseDelta += pos - _mousePosition;
            _mousePosition = pos;
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
