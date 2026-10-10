using System.Numerics;

namespace Novolis.Rendering.Planar;

/// <summary>Fake-isometric lift: gameplay XZ plus a shared height direction.</summary>
public static class PlanarElevation
{
    /// <summary>Default Loaded-style northwest extrusion.</summary>
    public static readonly Vector3 Direction = new(-0.28f, 0f, 0.62f);

    /// <summary>Offset for a wall or sprite height.</summary>
    public static Vector3 Offset(float height) => Direction * height;

    /// <summary>Gameplay position plus height along <see cref="Direction"/>.</summary>
    public static Vector3 Elevate(Vector3 gameplay, float elevation) => gameplay + Direction * elevation;
}
