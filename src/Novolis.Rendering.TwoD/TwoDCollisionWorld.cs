using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

/// <summary>Static collider set with circle-based movement resolution (platformer-friendly).</summary>
public sealed class TwoDCollisionWorld
{
    private readonly List<TwoDCollider> _static = [];

    /// <summary>Registered static colliders (platforms, blocks, pipes).</summary>
    public IReadOnlyList<TwoDCollider> StaticColliders => _static;

    /// <summary>Adds a static collider.</summary>
    /// <param name="collider">Collider to register.</param>
    public void AddStatic(TwoDCollider collider) => _static.Add(collider);

    /// <summary>Clears all static colliders.</summary>
    public void Clear() => _static.Clear();

    /// <summary>Whether a world point lies inside any solid collider.</summary>
    /// <param name="position">World position (Y ignored).</param>
    public bool ContainsPoint(Vector3 position)
    {
        foreach (var c in _static)
        {
            if (c.IsTrigger)
            {
                continue;
            }

            if (TwoDPlanarCollision.ContainsPoint(c.Shape, position.X, position.Z))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether a circle overlaps any solid collider.</summary>
    /// <param name="position">Circle center.</param>
    /// <param name="radius">Circle radius in world units.</param>
    public bool Overlaps(Vector3 position, float radius)
    {
        foreach (var c in _static)
        {
            if (c.IsTrigger)
            {
                continue;
            }

            if (TwoDPlanarCollision.CircleOverlaps(c.Shape, position.X, position.Z, radius))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Moves a circle actor with axis-separated resolution (Mario-style walk + gravity slides).
    /// </summary>
    /// <param name="position">Current position.</param>
    /// <param name="delta">Desired movement on X and Z.</param>
    /// <param name="radius">Actor collision radius.</param>
    /// <returns>Resolved position after collision.</returns>
    public Vector3 MoveCircle(Vector3 position, Vector3 delta, float radius)
    {
        var resolved = MoveAxis(position, new Vector3(delta.X, 0f, 0f), radius);
        resolved = MoveAxis(resolved, new Vector3(0f, 0f, delta.Z), radius);
        return resolved;
    }

    private Vector3 MoveAxis(Vector3 position, Vector3 axisDelta, float radius)
    {
        if (axisDelta == Vector3.Zero)
        {
            return position;
        }

        var lo = 0f;
        var hi = 1f;
        for (var i = 0; i < 16; i++)
        {
            var mid = (lo + hi) * 0.5f;
            if (Overlaps(position + axisDelta * mid, radius))
            {
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        return position + axisDelta * lo;
    }
}
