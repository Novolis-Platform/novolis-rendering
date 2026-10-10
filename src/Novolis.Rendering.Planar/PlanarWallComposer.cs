using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Math.Topology;

namespace Novolis.Rendering.Planar;

/// <summary>Draws a <see cref="PlanarPrism"/> into a planar scene (faces + cap, optional occlusion fade).</summary>
public static class PlanarWallComposer
{
    /// <summary>South/east bias used by Loaded-style ranges (skip back faces).</summary>
    public static bool IsFrontFace(Vector3 start, Vector3 end)
    {
        var edge = end - start;
        var outward = new Vector3(edge.Z, 0f, -edge.X);
        return Vector3.Dot(outward, new Vector3(0.15f, 0f, -1f)) > 0.05f
            || Vector3.Dot(outward, new Vector3(1f, 0f, 0f)) > 0.05f;
    }

    /// <summary>Adds front faces and the lifted cap. Returns faces that can occlude the observer.</summary>
    public static List<PlanarWallFace> Add(
        PlanarScene scene,
        PlanarPrism prism,
        PlanarTextureId texture,
        float textureMeters = 2f,
        bool frontFacesOnly = true)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(prism);
        var faces = new List<PlanarWallFace>();
        foreach (var side in prism.Sides)
        {
            if (frontFacesOnly && !IsFrontFace(side.Start, side.End))
            {
                continue;
            }

            var a = side.Start;
            var b = side.End;
            var ext = side.Offset;
            var shape = new Polygon([a, b, b + ext, a + ext]);
            var poly = new PlanarStaticPolygon(shape, Rgba32.White)
            {
                DrawFilled = true,
                Texture = texture,
                TextureMeters = textureMeters,
                SortKey = 40 + (int)(MathF.Min(a.Z, b.Z) * 4f),
            };
            scene.StaticPolygons.Add(poly);
            faces.Add(new PlanarWallFace
            {
                Polygon = poly,
                A = a,
                B = b,
                Offset = ext,
                Occludes = true,
            });
        }

        AddCap(scene, prism, texture, textureMeters);
        return faces;
    }

    /// <summary>Fades faces whose extrusion contains the observer. Optional muzzle brighten.</summary>
    public static void TickOcclusion(
        IReadOnlyList<PlanarWallFace> faces,
        Vector3 observer,
        float lightBoost = 1f)
    {
        ArgumentNullException.ThrowIfNull(faces);
        foreach (var face in faces)
        {
            var alpha = 255;
            if (face.Occludes && PointInExtrusion(observer, face.A, face.B, face.Offset))
            {
                alpha = 86;
            }

            face.Polygon.FillColor = new Rgba32(Mul(255, lightBoost), Mul(255, lightBoost), Mul(255, lightBoost), (byte)alpha);
        }
    }

    private static void AddCap(PlanarScene scene, PlanarPrism prism, PlanarTextureId texture, float textureMeters)
    {
        if (prism.Cap.Length < 3)
        {
            return;
        }

        var minZ = float.MaxValue;
        for (var i = 0; i < prism.Footprint.Length; i++)
        {
            minZ = MathF.Min(minZ, prism.Footprint[i].Z);
        }

        scene.StaticPolygons.Add(new PlanarStaticPolygon(prism.Cap, Rgba32.White)
        {
            DrawFilled = true,
            Texture = texture,
            TextureMeters = textureMeters,
            SortKey = 20 + (int)(minZ * 4f),
        });
    }

    private static byte Mul(int channel, float lit) => (byte)int.Clamp((int)(channel * lit), 0, 255);

    private static bool PointInExtrusion(Vector3 point, Vector3 a, Vector3 b, Vector3 ext)
    {
        var quad = new Polygon([a, b, b + ext, a + ext]);
        return quad.ContainsXz(point);
    }
}
