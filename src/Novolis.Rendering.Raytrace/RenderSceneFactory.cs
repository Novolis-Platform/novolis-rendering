using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.Abstractions;

namespace Novolis.Rendering.Raytrace;

/// <summary>Small procedural scenes for tests and dogfood demos.</summary>
public static class RenderSceneFactory
{
    public static RenderScene UnitCubeRoom()
    {
        var floor = new RenderMesh(
            [
                new(-1f, 0f, -1f),
                new(1f, 0f, -1f),
                new(1f, 0f, 1f),
                new(-1f, 0f, 1f),
            ],
            [0, 1, 2, 0, 2, 3],
            new Rgba32(90, 95, 105));

        var cube = new RenderMesh(
            [
                new(-0.25f, 0f, -0.25f),
                new(0.25f, 0f, -0.25f),
                new(0.25f, 0.5f, -0.25f),
                new(-0.25f, 0.5f, -0.25f),
                new(-0.25f, 0f, 0.25f),
                new(0.25f, 0f, 0.25f),
                new(0.25f, 0.5f, 0.25f),
                new(-0.25f, 0.5f, 0.25f),
            ],
            [
                0, 1, 2, 0, 2, 3,
                4, 6, 5, 4, 7, 6,
                0, 4, 5, 0, 5, 1,
                2, 6, 7, 2, 7, 3,
                0, 3, 7, 0, 7, 4,
                1, 5, 6, 1, 6, 2,
            ],
            new Rgba32(200, 120, 80));

        return new RenderScene(floor, cube);
    }
}
