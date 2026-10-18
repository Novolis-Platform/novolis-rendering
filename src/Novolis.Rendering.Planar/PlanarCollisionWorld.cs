using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.Planar;

/// <summary>Static collider set with circle-based movement resolution (platformer-friendly).</summary>
public sealed class PlanarCollisionWorld
{
    private readonly List<PlanarCollider> _static = [];
    private readonly Dictionary<(int X, int Z), List<int>> _cells = [];
    private int[] _queryMarks = [];
    private int _queryStamp;
    private const float BroadPhaseCellSize = 4f;

    /// <summary>Registered static colliders (platforms, blocks, pipes).</summary>
    public IReadOnlyList<PlanarCollider> StaticColliders => _static;

    /// <summary>Adds a static collider.</summary>
    /// <param name="collider">Collider to register.</param>
    public void AddStatic(PlanarCollider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);
        var index = _static.Count;
        _static.Add(collider);
        EnsureQueryCapacity();

        if (collider.Shape.Length == 0)
        {
            return;
        }

        var minX = collider.Shape[0].X;
        var maxX = minX;
        var minZ = collider.Shape[0].Z;
        var maxZ = minZ;
        for (var i = 1; i < collider.Shape.Length; i++)
        {
            var vertex = collider.Shape[i];
            minX = MathF.Min(minX, vertex.X);
            maxX = MathF.Max(maxX, vertex.X);
            minZ = MathF.Min(minZ, vertex.Z);
            maxZ = MathF.Max(maxZ, vertex.Z);
        }

        for (var x = ToCell(minX); x <= ToCell(maxX); x++)
        {
            for (var z = ToCell(minZ); z <= ToCell(maxZ); z++)
            {
                if (!_cells.TryGetValue((x, z), out var cell))
                {
                    cell = [];
                    _cells[(x, z)] = cell;
                }

                cell.Add(index);
            }
        }
    }

    /// <summary>Clears all static colliders.</summary>
    public void Clear()
    {
        _static.Clear();
        _cells.Clear();
        _queryMarks = [];
        _queryStamp = 0;
    }

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

            if (PlanarCollision.ContainsPoint(c.Shape, position.X, position.Z))
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
        var stamp = NextQueryStamp();
        var minX = ToCell(position.X - radius);
        var maxX = ToCell(position.X + radius);
        var minZ = ToCell(position.Z - radius);
        var maxZ = ToCell(position.Z + radius);
        for (var x = minX; x <= maxX; x++)
        {
            for (var z = minZ; z <= maxZ; z++)
            {
                if (!_cells.TryGetValue((x, z), out var cell))
                {
                    continue;
                }

                foreach (var index in cell)
                {
                    if (_queryMarks[index] == stamp)
                    {
                        continue;
                    }

                    _queryMarks[index] = stamp;
                    var collider = _static[index];
                    if (!collider.IsTrigger
                        && PlanarCollision.CircleOverlaps(collider.Shape, position.X, position.Z, radius))
                    {
                        return true;
                    }
                }
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

    private int NextQueryStamp()
    {
        if (++_queryStamp == int.MaxValue)
        {
            Array.Clear(_queryMarks);
            _queryStamp = 1;
        }

        return _queryStamp;
    }

    private void EnsureQueryCapacity()
    {
        if (_queryMarks.Length >= _static.Count)
        {
            return;
        }

        Array.Resize(ref _queryMarks, _static.Count);
    }

    private static int ToCell(float value) =>
        (int)MathF.Floor(value / BroadPhaseCellSize);
}
