namespace Novolis.Rendering.Abstractions;

/// <summary>Minimal scene graph: meshes only (lights and materials expand later).</summary>
public sealed class RenderScene
{
    public RenderScene(params RenderMesh[] meshes)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        Meshes = meshes;
    }

    public IReadOnlyList<RenderMesh> Meshes { get; }
}
