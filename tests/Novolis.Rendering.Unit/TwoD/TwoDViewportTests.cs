using System.Numerics;
using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDViewportTests
{
    [Test]
    public async Task ScreenToWorld_and_WorldToScreen_are_inverses_at_center()
    {
        var viewport = new TwoDViewport
        {
            Position = new Vector3(10f, 0f, 20f),
            ViewportWidth = 800,
            ViewportHeight = 600,
            WorldUnitsPerPixel = 0.05f,
        };

        var world = viewport.ScreenToWorld(400f, 300f);
        var back = viewport.WorldToScreen(world);

        await Assert.That(back.X).IsEqualTo(400f).Within(0.01f);
        await Assert.That(back.Z).IsEqualTo(300f).Within(0.01f);
    }

    [Test]
    public async Task GetViewProjectionMatrix_is_non_identity()
    {
        var viewport = new TwoDViewport
        {
            Position = Vector3.Zero,
            ViewportWidth = 640,
            ViewportHeight = 480,
            WorldUnitsPerPixel = 1f / 32f,
        };

        var matrix = viewport.GetViewProjectionMatrix();

        await Assert.That(matrix.M11).IsNotEqualTo(0f);
        await Assert.That(matrix.M22).IsNotEqualTo(0f);
    }
}
