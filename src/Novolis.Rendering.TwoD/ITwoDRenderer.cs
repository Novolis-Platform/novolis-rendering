namespace Novolis.Rendering.TwoD;

/// <summary>Backend contract for drawing a <see cref="TwoDScene"/>.</summary>
public interface ITwoDRenderer : IDisposable
{
    /// <summary>Resizes the internal framebuffer and viewport.</summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    void Resize(int width, int height);

    /// <summary>Draws the full scene for the current frame.</summary>
    /// <param name="scene">Scene to render.</param>
    void DrawScene(TwoDScene scene);
}
