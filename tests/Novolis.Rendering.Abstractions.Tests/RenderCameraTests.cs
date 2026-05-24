using System.Numerics;
using Novolis.Rendering.Abstractions;
using TUnit.Core;

namespace Novolis.Rendering.Abstractions.Tests;

public sealed class RenderCameraTests
{
    [Test]
    public async Task LookAt_ForwardPointsAtTarget()
    {
        var camera = RenderCamera.LookAt(
            new Vector3(0f, 2f, 5f),
            Vector3.Zero,
            Vector3.UnitY,
            60f,
            16f / 9f);

        var expectedForward = Vector3.Normalize(-new Vector3(0f, 2f, 5f));
        await Assert.That(Vector3.Dot(camera.Forward, expectedForward)).IsGreaterThan(0.999f);
        await Assert.That(camera.AspectRatio).IsEqualTo(16f / 9f);
    }
}
