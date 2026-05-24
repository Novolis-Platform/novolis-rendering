namespace Novolis.Rendering.Abstractions;

/// <summary>Produces a CPU framebuffer; does not present pixels to a window or GPU.</summary>
public interface IRayTracer
{
    /// <summary>Fills <paramref name="target"/> from <paramref name="camera"/> viewing <paramref name="scene"/>.</summary>
    void Render(ImageBuffer target, in RenderCamera camera, RenderScene scene);
}
