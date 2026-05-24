using Novolis.Rendering.Presentation.Silk;
using TUnit.Core;

namespace Novolis.Rendering.Presentation.Silk.Tests;

public sealed class SilkOrbitCameraTests
{
    [Test]
    public async Task AdjustDistance_clamps_to_max()
    {
        var cam = new SilkOrbitCamera { Distance = 11f, MaxDistance = 10f };
        cam.AdjustDistance(5f);
        await Assert.That(cam.Distance).IsEqualTo(10f);
    }

    [Test]
    public async Task BuildEyePosition_is_offset_from_target()
    {
        var cam = new SilkOrbitCamera
        {
            Target = System.Numerics.Vector3.Zero,
            Distance = 2f,
            Yaw = 0f,
            Pitch = 0f,
        };
        var eye = cam.BuildEyePosition();
        await Assert.That(eye.Length()).IsGreaterThan(1.5f);
    }
}
