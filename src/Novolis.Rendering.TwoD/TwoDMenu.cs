namespace Novolis.Rendering.TwoD;

/// <summary>Stack of menu screens (title, pause, game over).</summary>
public sealed class TwoDMenuStack
{
    private readonly Stack<TwoDMenuScreen> _screens = new();

    /// <summary>Whether any menu is visible.</summary>
    public bool IsActive => _screens.Count > 0;

    /// <summary>Top-most menu screen, if any.</summary>
    public TwoDMenuScreen? Active => _screens.TryPeek(out var s) ? s : null;

    /// <summary>Pushes a new screen on top.</summary>
    /// <param name="screen">Menu screen.</param>
    public void Push(TwoDMenuScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screens.Push(screen);
        screen.FocusIndex = 0;
    }

    /// <summary>Removes the top screen.</summary>
    /// <returns><see langword="true"/> when a screen was removed.</returns>
    public bool Pop() => _screens.Count > 0 && _screens.Pop() is not null;

    /// <summary>Clears all screens.</summary>
    public void Clear() => _screens.Clear();

    /// <summary>Moves focus up or down on the active screen.</summary>
    /// <param name="direction">-1 = up, +1 = down.</param>
    public void Navigate(int direction)
    {
        if (Active is not { } screen || screen.Items.Count == 0)
        {
            return;
        }

        var count = screen.Items.Count;
        screen.FocusIndex = (screen.FocusIndex + direction + count) % count;
    }

    /// <summary>Invokes the focused item's action and returns its tag.</summary>
    public object? Select()
    {
        if (Active is not { } screen || screen.Items.Count == 0)
        {
            return null;
        }

        return screen.Items[screen.FocusIndex].OnSelect?.Invoke();
    }

    /// <summary>Applies host-free menu input (up/down/confirm/cancel).</summary>
    public void HandleInput(bool up, bool down, bool confirm, bool cancel)
    {
        if (!IsActive)
        {
            return;
        }

        if (up)
        {
            Navigate(-1);
        }
        else if (down)
        {
            Navigate(1);
        }
        else if (confirm)
        {
            Select();
        }
        else if (cancel)
        {
            Pop();
        }
    }
}
