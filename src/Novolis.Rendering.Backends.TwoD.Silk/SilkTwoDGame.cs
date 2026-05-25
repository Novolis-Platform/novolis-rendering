using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

namespace Novolis.Rendering.Backends.TwoD.Silk;

/// <summary>Silk.NET window loop for 2D platformer-style games.</summary>
public static class SilkTwoDGame
{
    /// <summary>Runs a 2D game with initialize and per-frame update callbacks.</summary>
    /// <param name="title">Window title.</param>
    /// <param name="width">Initial width in pixels.</param>
    /// <param name="height">Initial height in pixels.</param>
    /// <param name="initialize">One-time setup before the first update.</param>
    /// <param name="update">Per-frame update (movement, animation, menu input).</param>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkTwoDGameContext>? initialize,
        Action<SilkTwoDGameContext> update)
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
        var ctx = new SilkTwoDGameContext();
        SilkTwoDRenderer? renderer = null;
        var initialized = false;

        window.Load += () =>
        {
            var gl = GL.GetApi(window);
            renderer = new SilkTwoDRenderer(gl);
            ctx.Bind(window, renderer, window.CreateInput());
        };

        window.Update += delta =>
        {
            ctx.SetFrame(window.FramebufferSize, (float)delta);
            if (!initialized)
            {
                initialize?.Invoke(ctx);
                initialized = true;
            }

            HandleMenuInput(ctx);
            update(ctx);
            ctx.EndInputFrame();
        };

        void ReleaseRenderer()
        {
            if (renderer is null)
            {
                return;
            }

            window.GLContext?.MakeCurrent();
            renderer.Dispose();
            renderer = null;
        }

        window.Render += _ =>
        {
            if (renderer is null)
            {
                return;
            }

            renderer.DrawScene(ctx.Scene);
            if (window.IsClosing)
            {
                ReleaseRenderer();
            }
        };

        window.Closing += ReleaseRenderer;

        window.Run();
    }

    private static void HandleMenuInput(SilkTwoDGameContext ctx)
    {
        if (!ctx.Scene.Menus.IsActive)
        {
            return;
        }

        if (ctx.IsMenuUpPressed())
        {
            ctx.Scene.Menus.Navigate(-1);
        }
        else if (ctx.IsMenuDownPressed())
        {
            ctx.Scene.Menus.Navigate(1);
        }
        else if (ctx.IsMenuConfirmPressed())
        {
            ctx.Scene.Menus.Select();
        }
        else if (ctx.IsMenuCancelPressed())
        {
            ctx.Scene.Menus.Pop();
        }
    }
}
