using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Math.Topology;
using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarComposeTests
{
    [Test]
    public async Task Stamp_DiscHasCenterAlpha()
    {
        var registry = new PlanarTextureRegistry();
        var id = registry.Disc(8, new Rgba32(10, 20, 30, 200), falloff: 1f, "d");
        var px = new Rgba32[64];
        registry.CopyPixels(id, px, out var w, out var h);
        await Assert.That(w).IsEqualTo(8);
        await Assert.That(h).IsEqualTo(8);
        await Assert.That(px[4 * 8 + 4].A).IsGreaterThan((byte)100);
    }

    [Test]
    public async Task ParticleField_ExpiresAndBounces()
    {
        var field = new SpriteParticleField(8);
        field.Emit(new SpriteParticle
        {
            MaxLife = 0.05f,
            Elevation = 0.2f,
            ElevationVelocity = -2f,
        });
        field.Tick(0.1f);
        await Assert.That(field.Count).IsEqualTo(0);

        field.Emit(new SpriteParticle { MaxLife = 1f, Elevation = 0.01f, ElevationVelocity = -1f });
        field.Tick(0.02f);
        await Assert.That(field.Span[0].Elevation).IsGreaterThanOrEqualTo(0f);
    }

    [Test]
    public async Task DecalField_OverwritesWhenFull()
    {
        var field = new SpriteDecalField(2);
        field.Add(new SpriteDecal { Width = 1f });
        field.Add(new SpriteDecal { Width = 2f });
        field.Add(new SpriteDecal { Width = 3f });
        await Assert.That(field.Count).IsEqualTo(2);
        await Assert.That(field.Span[0].Width).IsEqualTo(3f);
    }

    [Test]
    public async Task WallComposer_AddsFrontFaceAndCap()
    {
        var scene = new PlanarScene();
        var footprint = new Polygon(
        [
            new Vector3(0f, 0f, 0f),
            new Vector3(4f, 0f, 0f),
            new Vector3(4f, 0f, 1f),
            new Vector3(0f, 0f, 1f),
        ]);
        var prism = footprint.Extrude(PlanarElevation.Offset(2f));
        var faces = PlanarWallComposer.Add(scene, prism, default);
        await Assert.That(faces.Count).IsGreaterThan(0);
        await Assert.That(scene.StaticPolygons.Count).IsGreaterThan(faces.Count);
    }

    [Test]
    public async Task SpritePool_HidesUnused()
    {
        var scene = new PlanarScene();
        var pool = new SpritePool();
        pool.Ensure(scene, 3, default, 10);
        pool.HideFrom(1);
        await Assert.That(scene.Sprites.Count).IsEqualTo(3);
        await Assert.That(pool.Items[2].Transform.Scale).IsEqualTo(Vector3.Zero);
    }
}
