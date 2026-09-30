namespace Novolis.Rendering.TwoD;

/// <summary>Single menu screen with a title and selectable items.</summary>
public sealed class TwoDMenuScreen
{
    /// <summary>Creates a menu screen.</summary>
    /// <param name="title">Heading text.</param>
    /// <param name="items">Selectable rows.</param>
    public TwoDMenuScreen(string title, IReadOnlyList<TwoDMenuItem> items)
    {
        Title = title;
        Items = items;
    }

    /// <summary>Screen heading.</summary>
    public string Title { get; }

    /// <summary>Menu rows.</summary>
    public IReadOnlyList<TwoDMenuItem> Items { get; }

    /// <summary>Index of the focused item.</summary>
    public int FocusIndex { get; set; }

    /// <summary>Dim overlay behind the menu.</summary>
    public bool DimBackground { get; set; } = true;
}
