namespace Novolis.Rendering.Planar;

/// <summary>Single menu screen with a title and selectable items.</summary>
public sealed class PlanarMenuScreen
{
    /// <summary>Creates a menu screen.</summary>
    /// <param name="title">Heading text.</param>
    /// <param name="items">Selectable rows.</param>
    public PlanarMenuScreen(string title, IReadOnlyList<PlanarMenuItem> items)
    {
        Title = title;
        Items = items;
    }

    /// <summary>Screen heading.</summary>
    public string Title { get; }

    /// <summary>Menu rows.</summary>
    public IReadOnlyList<PlanarMenuItem> Items { get; }

    /// <summary>Index of the focused item.</summary>
    public int FocusIndex { get; set; }

    /// <summary>Dim overlay behind the menu.</summary>
    public bool DimBackground { get; set; } = true;
}
