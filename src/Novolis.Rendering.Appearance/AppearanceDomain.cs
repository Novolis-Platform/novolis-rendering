namespace Novolis.Rendering.Appearance;

/// <summary>Semantic domain for an appearance stack. Do not mix fog and shell casings on one stack.</summary>
public enum AppearanceDomain
{
    /// <summary>Evaluated at a surface sample (albedo, roughness, emission, …).</summary>
    Surface = 0,

    /// <summary>Evaluated through space (density, scatter, absorption).</summary>
    Volume = 1,

    /// <summary>Produces illumination (point, cone, flicker, falloff).</summary>
    Light = 2,

    /// <summary>Transient energy or geometry look (beam, projectile, sparks).</summary>
    Effect = 3,

    /// <summary>Operates on a finished image (bloom, exposure, vignette).</summary>
    Post = 4,
}
