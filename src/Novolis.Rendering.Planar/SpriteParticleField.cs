namespace Novolis.Rendering.Planar;

/// <summary>Fixed-capacity cosmetic particle pool with drag and floor bounce.</summary>
public sealed class SpriteParticleField
{
    /// <summary>Default pool size (FrankMoat-scale FX).</summary>
    public const int DefaultCapacity = 12000;

    private readonly SpriteParticle[] _items;
    private int _count;

    /// <summary>Creates a pool of <paramref name="capacity"/> slots.</summary>
    public SpriteParticleField(int capacity = DefaultCapacity) =>
        _items = new SpriteParticle[int.Max(1, capacity)];

    /// <summary>Live particle count.</summary>
    public int Count => _count;

    /// <summary>Live slice.</summary>
    public ReadOnlySpan<SpriteParticle> Span => _items.AsSpan(0, _count);

    /// <summary>Removes every particle.</summary>
    public void Clear() => _count = 0;

    /// <summary>Adds a particle; overwrites a random slot when full.</summary>
    public void Emit(in SpriteParticle particle)
    {
        var next = WithLife(particle);
        if (_count >= _items.Length)
        {
            _items[Random.Shared.Next(_count)] = next;
            return;
        }

        _items[_count++] = next;
    }

    /// <summary>Integrates life, drag, elevation bounce, and spin.</summary>
    public void Tick(float dt)
    {
        for (var i = _count - 1; i >= 0; i--)
        {
            ref var p = ref _items[i];
            p.Life -= dt;
            if (p.Life <= 0f)
            {
                _items[i] = _items[--_count];
                continue;
            }

            if (!p.Sticky)
            {
                var drag = MathF.Exp(-p.Drag * dt);
                p.Velocity *= drag;
                p.Position += p.Velocity * dt;
                p.ElevationVelocity -= 9.2f * dt;
                p.Elevation = MathF.Max(0f, p.Elevation + p.ElevationVelocity * dt);
                if (p.Elevation <= 0f)
                {
                    p.ElevationVelocity *= -0.18f;
                    p.Velocity *= 0.55f;
                }
            }

            p.Rotation += p.Spin * dt;
        }
    }

    private static SpriteParticle WithLife(SpriteParticle particle)
    {
        if (particle.MaxLife > 0f)
        {
            particle.Life = particle.MaxLife;
        }

        return particle;
    }
}
