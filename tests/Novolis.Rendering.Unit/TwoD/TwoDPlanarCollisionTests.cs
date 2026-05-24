using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Math.Topology;
using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDPlanarCollisionTests
{
    [Test]
    public async Task ContainsPoint_InsideUnitSquare_ReturnsTrue()
    {
        var square = TwoDScenePrimitives.Rectangle(0, 0, 1, 1);
        await Assert.That(TwoDPlanarCollision.ContainsPoint(square, 0.5f, 0.5f)).IsTrue();
    }

    [Test]
    public async Task ContainsPoint_OutsideUnitSquare_ReturnsFalse()
    {
        var square = TwoDScenePrimitives.Rectangle(0, 0, 1, 1);
        await Assert.That(TwoDPlanarCollision.ContainsPoint(square, 2f, 2f)).IsFalse();
    }

    [Test]
    public async Task MoveCircle_StopsAtPlatform()
    {
        var world = new TwoDCollisionWorld();
        world.AddStatic(new TwoDCollider(TwoDScenePrimitives.Rectangle(0, 0, 4, 1)));
        var start = Vector3PlanarExtensions.Xz(1, 2);
        var moved = world.MoveCircle(start, Vector3PlanarExtensions.Xz(0, -3), radius: 0.4f);
        await Assert.That(moved.Z).IsGreaterThan(1f);
    }
}
