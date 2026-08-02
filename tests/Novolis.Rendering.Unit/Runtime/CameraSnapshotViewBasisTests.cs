using System.Numerics;
using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.Runtime.Tests;

public sealed class CameraSnapshotViewBasisTests
{
    [Test]
    public async Task ToViewBasis_preserves_camera_axes()
    {
        var snapshot = CameraSnapshot.LookAt(
            new Vector3(0f, 2f, 5f),
            Vector3.Zero,
            Vector3.UnitY,
            verticalFovDegrees: 60f,
            aspectRatio: 16f / 9f);

        var basis = CameraSnapshotViewBasis.ToViewBasis(snapshot);

        await Assert.That(basis.Forward).IsEqualTo(snapshot.Forward);
        await Assert.That(basis.Right).IsEqualTo(snapshot.Right);
        await Assert.That(basis.Up).IsEqualTo(snapshot.Up);
    }

    [Test]
    public async Task PrimaryRayDirection_center_points_forward()
    {
        var snapshot = CameraSnapshot.FromObserver(
            Vector3.Zero,
            -Vector3.UnitZ,
            Vector3.UnitY,
            fieldOfViewDegrees: 90f,
            aspectRatio: 1f);

        var direction = CameraSnapshotViewBasis.PrimaryRayDirection(snapshot, u: 0f, v: 0f);

        await Assert.That(Vector3.Dot(direction, snapshot.Forward)).IsGreaterThan(0.9f);
    }

    [Test]
    public async Task LookAt_degenerates_when_target_equals_position()
    {
        var snapshot = CameraSnapshot.LookAt(
            Vector3.Zero,
            Vector3.Zero,
            Vector3.UnitY,
            verticalFovDegrees: 45f,
            aspectRatio: 1f);

        await Assert.That(snapshot.Forward.Length()).IsEqualTo(1f).Within(0.001f);
    }
}
