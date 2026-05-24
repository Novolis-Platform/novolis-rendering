using System.Numerics;

namespace Novolis.Rendering.Presentation.Silk;

/// <summary>Yaw/pitch orbit rig for interactive Silk path-tracing demos.</summary>
public sealed class SilkOrbitCamera
{
    private const float PitchLimit = MathF.PI * 0.49f;

    /// <summary>Orbit target in world space.</summary>
    public Vector3 Target { get; set; }

    /// <summary>Orbit radius in world units.</summary>
    public float Distance { get; set; } = 2.4f;

    /// <summary>Minimum orbit distance.</summary>
    public float MinDistance { get; set; } = 0.6f;

    /// <summary>Maximum orbit distance.</summary>
    public float MaxDistance { get; set; } = 12f;

    /// <summary>Horizontal angle in radians.</summary>
    public float Yaw { get; set; }

    /// <summary>Vertical angle in radians.</summary>
    public float Pitch { get; set; } = 0.35f;

    /// <summary>Vertical field of view in degrees.</summary>
    public float FieldOfViewDegrees { get; set; } = 52f;

    /// <summary>Applies mouse-look style deltas.</summary>
    /// <param name="deltaYaw">Yaw delta in radians.</param>
    /// <param name="deltaPitch">Pitch delta in radians.</param>
    public void AddLookDelta(float deltaYaw, float deltaPitch)
    {
        Yaw += deltaYaw;
        Pitch += deltaPitch;
        Pitch = System.Math.Clamp(Pitch, -0.1f, PitchLimit);
    }

    /// <summary>Adjusts orbit distance (e.g. mouse wheel).</summary>
    /// <param name="delta">Signed distance change in world units.</param>
    public void AdjustDistance(float delta) =>
        Distance = System.Math.Clamp(Distance + delta, MinDistance, MaxDistance);

    /// <summary>Builds an eye position orbiting <see cref="Target"/>.</summary>
    /// <returns>World-space eye position.</returns>
    public Vector3 BuildEyePosition()
    {
        var cosP = MathF.Cos(Pitch);
        return Target + new Vector3(
            MathF.Sin(Yaw) * cosP * Distance,
            MathF.Sin(Pitch) * Distance,
            MathF.Cos(Yaw) * cosP * Distance);
    }

}
