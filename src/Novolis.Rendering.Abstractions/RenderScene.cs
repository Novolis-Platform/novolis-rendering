namespace Novolis.Rendering.Abstractions;

/// <summary>Minimal scene graph: meshes only (lights and materials expand later).</summary>
[Obsolete("Use Novolis.Rendering.Scene.Scene.")]
public sealed class RenderScene
{
    /// <summary>Creates a scene from one or more meshes.</summary>
    /// <param name="meshes">Meshes to include (must not be null).</param>
    public RenderScene(params RenderMesh[] meshes)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        Meshes = meshes;
    }

    /// <summary>Meshes in this scene.</summary>
    public IReadOnlyList<RenderMesh> Meshes { get; }
}
