using Silk.NET.Input;
using Silk.NET.Windowing.Glfw;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Novolis.Rendering.Presentation.Silk;

/// <summary>Minimal Silk.NET window loop for path-tracing demos.</summary>
public static class SilkGame
{
    /// <summary>Runs a windowed demo with an optional one-time initializer.</summary>
    /// <param name="title">Window title.</param>
    /// <param name="width">Initial client width.</param>
    /// <param name="height">Initial client height.</param>
    /// <param name="update">Per-frame update callback.</param>
    public static void Run(string title, int width, int height, Action<SilkGameContext> update) =>
        Run(title, width, height, null, update);

    /// <summary>Runs a windowed demo with initialize and update callbacks.</summary>
    /// <param name="title">Window title.</param>
    /// <param name="width">Initial client width.</param>
    /// <param name="height">Initial client height.</param>
    /// <param name="initialize">One-time setup invoked before the first update.</param>
    /// <param name="update">Per-frame update callback.</param>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkGameContext>? initialize,
        Action<SilkGameContext> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        var options = WindowOptions.Default with
        {
            Title = title,
            Size = new Vector2D<int>(width, height),
            API = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
                ContextFlags.ForwardCompatible,
                new APIVersion(3, 3)),
            VSync = true,
        };

        GlfwWindowing.RegisterPlatform();
        using var window = Window.Create(options);
        var ctx = new SilkGameContext();
        var presenter = new SilkOpenGlFramePresenter(window);
        var initialized = false;

        window.Load += () =>
        {
            var gl = GL.GetApi(window);
            presenter.Initialize(gl);
            ctx.Bind(window, presenter, window.CreateInput());
        };

        window.Update += delta =>
        {
            ctx.SetFrame(window.FramebufferSize, (float)delta);
            if (!initialized)
            {
                initialize?.Invoke(ctx);
                initialized = true;
            }

            update(ctx);
            ctx.EndInputFrame();
        };

        window.Render += _ => presenter.Draw();
        window.Run();
        window.Dispose();
    }
}
