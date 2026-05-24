namespace Novolis.Rendering.Scene;

/// <summary>Authoring scene graph (meshes and lights).</summary>
public sealed class Scene
{
    /// <summary>Creates a scene from mesh instances and lights.</summary>
    /// <param name="meshes">Mesh instances (must not be null).</param>
    /// <param name="lights">Light definitions (must not be null).</param>
    public Scene(IReadOnlyList<MeshInstance> meshes, IReadOnlyList<LightDefinition> lights)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(lights);
        Meshes = meshes;
        Lights = lights;
    }

    /// <summary>Mesh instances in authoring space.</summary>
    public IReadOnlyList<MeshInstance> Meshes { get; }

    /// <summary>Light definitions.</summary>
    public IReadOnlyList<LightDefinition> Lights { get; }
}
