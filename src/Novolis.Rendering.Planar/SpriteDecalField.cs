namespace Novolis.Rendering.Planar;

/// <summary>Ring-buffer of persistent marks.</summary>
public sealed class SpriteDecalField
{
    /// <summary>Default overwrite capacity.</summary>
    public const int DefaultCapacity = 2800;

    private readonly SpriteDecal[] _items;
    private int _count;
    private int _cursor;

    /// <summary>Creates a ring of <paramref name="capacity"/> slots.</summary>
    public SpriteDecalField(int capacity = DefaultCapacity) =>
        _items = new SpriteDecal[int.Max(1, capacity)];

    /// <summary>How many slots are filled (caps at capacity).</summary>
    public int Count => _count;

    /// <summary>Filled slice (wrap overwrite stays inside this length once full).</summary>
    public ReadOnlySpan<SpriteDecal> Span => _items.AsSpan(0, _count);

    /// <summary>Clears every mark.</summary>
    public void Clear()
    {
        _count = 0;
        _cursor = 0;
    }

    /// <summary>Appends, then overwrites from the start when full.</summary>
    public void Add(in SpriteDecal decal)
    {
        if (_count < _items.Length)
        {
            _items[_count++] = decal;
            _cursor = _count % _items.Length;
            return;
        }

        _items[_cursor] = decal;
        _cursor = (_cursor + 1) % _items.Length;
    }
}
