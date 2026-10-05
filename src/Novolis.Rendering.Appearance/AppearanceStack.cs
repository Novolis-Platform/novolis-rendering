namespace Novolis.Rendering.Appearance;

/// <summary>Ordered, serializable appearance graph for one <see cref="AppearanceDomain"/>.</summary>
public sealed class AppearanceStack
{
    /// <summary>Creates a stack. Empty layers are treated as identity.</summary>
    public AppearanceStack(AppearanceDomain domain, IReadOnlyList<AppearanceLayer> layers)
    {
        Domain = domain;
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
    }

    /// <summary>Semantic domain. Hosts compose domains; a stack does not.</summary>
    public AppearanceDomain Domain { get; }

    /// <summary>Operations from first to last.</summary>
    public IReadOnlyList<AppearanceLayer> Layers { get; }

    /// <summary>Grey surface with no further ops.</summary>
    public static AppearanceStack GreySurface() =>
        new(AppearanceDomain.Surface, [new AppearanceLayer(AppearanceOp.Identity)]);

    /// <summary>Empty volume.</summary>
    public static AppearanceStack EmptyVolume() =>
        new(AppearanceDomain.Volume, [new AppearanceLayer(AppearanceOp.Identity)]);

    /// <summary>Unit white light.</summary>
    public static AppearanceStack WhiteLight() =>
        new(AppearanceDomain.Light, [new AppearanceLayer(AppearanceOp.Identity)]);

    /// <summary>Empty effect.</summary>
    public static AppearanceStack EmptyEffect() =>
        new(AppearanceDomain.Effect, [new AppearanceLayer(AppearanceOp.Identity)]);

    /// <summary>Passthrough post.</summary>
    public static AppearanceStack PassthroughPost() =>
        new(AppearanceDomain.Post, [new AppearanceLayer(AppearanceOp.Identity)]);

    /// <summary>Appends a layer (immutable).</summary>
    public AppearanceStack Then(
        AppearanceOp op,
        float amount = 1f,
        float scale = 1f,
        Novolis.Math.Geometry.Rgba32 color = default,
        int seed = 0,
        float aux = 0f)
    {
        var next = new AppearanceLayer[Layers.Count + 1];
        for (var i = 0; i < Layers.Count; i++)
        {
            next[i] = Layers[i];
        }

        next[Layers.Count] = new AppearanceLayer(op, amount, scale, color, seed, aux);
        return new AppearanceStack(Domain, next);
    }
}
