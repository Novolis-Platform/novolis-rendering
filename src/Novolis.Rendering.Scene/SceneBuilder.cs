using System.Numerics;
using Novolis.Rendering.Materials;

namespace Novolis.Rendering.Scene;

/// <summary>Fluent scene construction helpers.</summary>
public sealed class SceneBuilder
{
    private readonly List<MeshInstance> _meshes = [];
    private readonly List<LightDefinition> _lights = [];

    public SceneBuilder AddMesh(Vector3[] vertices, int[] indices, IMaterial material, Matrix4x4 transform = default)
    {
        _meshes.Add(new MeshInstance(vertices, indices, material, transform == default ? Matrix4x4.Identity : transform));
        return this;
    }

    public SceneBuilder AddGround(IMaterial material, float size = 2f)
    {
        var h = size * 0.5f;
        return AddMesh(
            [
                new(-h, 0f, -h),
                new(h, 0f, -h),
                new(h, 0f, h),
                new(-h, 0f, h),
            ],
            [0, 1, 2, 0, 2, 3],
            material);
    }

    public SceneBuilder AddBox(Vector3 center, Vector3 halfExtents, IMaterial material)
    {
        var x = halfExtents.X;
        var y = halfExtents.Y;
        var z = halfExtents.Z;
        var c = center;
        var verts = new Vector3[]
        {
            c + new Vector3(-x, -y, -z), c + new Vector3(x, -y, -z), c + new Vector3(x, y, -z), c + new Vector3(-x, y, -z),
            c + new Vector3(-x, -y, z), c + new Vector3(x, -y, z), c + new Vector3(x, y, z), c + new Vector3(-x, y, z),
        };
        var indices = new[]
        {
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6,
            0, 4, 5, 0, 5, 1,
            2, 6, 7, 2, 7, 3,
            0, 3, 7, 0, 7, 4,
            1, 5, 6, 1, 6, 2,
        };
        return AddMesh(verts, indices, material);
    }

    public SceneBuilder AddDirectionalLight(Vector3 direction, Vector3 color, float intensity = 1f)
    {
        _lights.Add(new LightDefinition(LightKind.Directional, Vector3.Normalize(direction), color, intensity));
        return this;
    }

    public Scene Build() => new(_meshes, _lights);
}
