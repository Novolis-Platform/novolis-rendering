using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Math.Topology;
using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarCollisionTests
{
    [Test]
    public async Task ContainsPoint_InsideUnitSquare_ReturnsTrue()
    {
        var square = PlanarScenePrimitives.Rectangle(0, 0, 1, 1);
        await Assert.That(PlanarCollision.ContainsPoint(square, 0.5f, 0.5f)).IsTrue();
    }

    [Test]
    public async Task ContainsPoint_OutsideUnitSquare_ReturnsFalse()
    {
        var square = PlanarScenePrimitives.Rectangle(0, 0, 1, 1);
        await Assert.That(PlanarCollision.ContainsPoint(square, 2f, 2f)).IsFalse();
    }

    [Test]
    public async Task MoveCircle_StopsAtPlatform()
    {
        var world = new PlanarCollisionWorld();
        world.AddStatic(new PlanarCollider(PlanarScenePrimitives.Rectangle(0, 0, 4, 1)));
        var start = Vector3PlanarExtensions.Xz(1, 2);
        var moved = world.MoveCircle(start, Vector3PlanarExtensions.Xz(0, -3), radius: 0.4f);
        await Assert.That(moved.Z).IsGreaterThan(1f);
    }
}
