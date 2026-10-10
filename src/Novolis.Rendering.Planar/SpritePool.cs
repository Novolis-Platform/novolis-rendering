using System.Numerics;

namespace Novolis.Rendering.Planar;

/// <summary>Grows <see cref="PlanarScene.Sprites"/> and hides extras with zero scale.</summary>
public sealed class SpritePool
{
    private readonly List<PlanarSpriteInstance> _items = [];

    /// <summary>Rented sprites in order.</summary>
    public IReadOnlyList<PlanarSpriteInstance> Items => _items;

    /// <summary>Ensures at least <paramref name="count"/> sprites exist on the scene.</summary>
    public void Ensure(PlanarScene scene, int count, PlanarTextureId texture, int sortKey)
    {
        ArgumentNullException.ThrowIfNull(scene);
        while (_items.Count < count)
        {
            var sprite = new PlanarSpriteInstance
            {
                Texture = texture,
                SortKey = sortKey,
            };
            scene.Sprites.Add(sprite);
            _items.Add(sprite);
        }
    }

    /// <summary>Hides sprites from <paramref name="used"/> onward.</summary>
    public void HideFrom(int used)
    {
        for (var i = used; i < _items.Count; i++)
        {
            _items[i].Transform.Scale = Vector3.Zero;
        }
    }

    /// <summary>Removes every rented sprite from the scene.</summary>
    public void Clear(PlanarScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        foreach (var sprite in _items)
        {
            scene.Sprites.Remove(sprite);
        }

        _items.Clear();
    }
}
