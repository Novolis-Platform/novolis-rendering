using System.Numerics;

namespace Novolis.Rendering.Appearance;

/// <summary>Evaluation inputs. No textures, no GPU types.</summary>
public readonly record struct AppearanceContext
{
    /// <summary>World position (meters).</summary>
    public Vector3 Position { get; init; }

    /// <summary>Unit surface normal (unused for volume/post).</summary>
    public Vector3 Normal { get; init; }

    /// <summary>Unit view direction toward the camera.</summary>
    public Vector3 View { get; init; }

    /// <summary>Parametric U (surface UV or light local X).</summary>
    public float U { get; init; }

    /// <summary>Parametric V (surface UV or light local Y).</summary>
    public float V { get; init; }

    /// <summary>Seconds.</summary>
    public float Time { get; init; }

    /// <summary>Deterministic seed.</summary>
    public int Seed { get; init; }

    /// <summary>Distance from a light origin (meters).</summary>
    public float Distance { get; init; }

    /// <summary>Cosine of angle from a light axis (cone lights).</summary>
    public float AxisCosine { get; init; }
}
