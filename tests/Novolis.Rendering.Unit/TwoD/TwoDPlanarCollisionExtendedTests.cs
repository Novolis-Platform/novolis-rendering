using System.Numerics;
using Novolis.Math.Topology;
using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDPlanarCollisionExtendedTests
{
    [Test]
    public async Task ContainsPoint_degenerate_polygon_returns_false()
    {
        var line = new Polygon([new Vector3(0, 0, 0), new Vector3(1, 0, 1)]);
        await Assert.That(TwoDPlanarCollision.ContainsPoint(line, 0.5f, 0.5f)).IsFalse();
    }

    [Test]
    public async Task CircleOverlaps_detects_edge_proximity()
    {
        var square = TwoDScenePrimitives.Rectangle(0, 0, 2, 2);
        await Assert.That(TwoDPlanarCollision.CircleOverlaps(square, 2.2f, 1f, 0.5f)).IsTrue();
        await Assert.That(TwoDPlanarCollision.CircleOverlaps(square, 5f, 5f, 0.1f)).IsFalse();
    }

    [Test]
    public async Task TryGetCircleSeparation_pushes_outside_edge_overlap()
    {
        var square = TwoDScenePrimitives.Rectangle(0, 0, 2, 2);
        var overlapped = TwoDPlanarCollision.TryGetCircleSeparation(square, 2.1f, 1f, 0.5f, out var separation);
        await Assert.That(overlapped).IsTrue();
        await Assert.That(separation.Length()).IsGreaterThan(0f);
    }

    [Test]
    public async Task TryGetCircleSeparation_pushes_out_of_interior()
    {
        var square = TwoDScenePrimitives.Rectangle(0, 0, 4, 4);
        var overlapped = TwoDPlanarCollision.TryGetCircleSeparation(square, 2f, 2f, 0.5f, out var separation);
        await Assert.That(overlapped).IsTrue();
        await Assert.That(separation.Length()).IsGreaterThan(0f);
    }

    [Test]
    public async Task CollisionWorld_skips_triggers_and_resolves_x_move()
    {
        var world = new TwoDCollisionWorld();
        world.AddStatic(new TwoDCollider(TwoDScenePrimitives.Rectangle(0, 0, 4, 1), isTrigger: true));
        world.AddStatic(new TwoDCollider(TwoDScenePrimitives.Rectangle(0, 0, 2, 2)));

        await Assert.That(world.ContainsPoint(new Vector3(1, 0, 1))).IsTrue();
        await Assert.That(world.Overlaps(new Vector3(1, 0, 1), 0.2f)).IsTrue();

        var start = new Vector3(3f, 0f, 1f);
        var moved = world.MoveCircle(start, new Vector3(-2f, 0f, 0f), radius: 0.3f);
        await Assert.That(moved.X).IsGreaterThan(2f);
    }
}
