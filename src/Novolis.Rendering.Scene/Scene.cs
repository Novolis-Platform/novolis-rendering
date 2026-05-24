namespace Novolis.Rendering.Scene;

/// <summary>Authoring scene graph (meshes and lights).</summary>
public sealed class Scene
{
    public Scene(IReadOnlyList<MeshInstance> meshes, IReadOnlyList<LightDefinition> lights)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(lights);
        Meshes = meshes;
        Lights = lights;
    }

    public IReadOnlyList<MeshInstance> Meshes { get; }
    public IReadOnlyList<LightDefinition> Lights { get; }
}
