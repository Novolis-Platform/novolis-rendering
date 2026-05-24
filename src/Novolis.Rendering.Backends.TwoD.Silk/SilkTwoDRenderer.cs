using System.Numerics;
using System.Runtime.InteropServices;
using Novolis.Math.Geometry;
using Novolis.Rendering.TwoD;
using Silk.NET.OpenGL;

namespace Novolis.Rendering.Backends.TwoD.Silk;

/// <summary>Silk.NET OpenGL implementation of <see cref="ITwoDRenderer"/>.</summary>
public sealed class SilkTwoDRenderer : ITwoDRenderer
{
    private const string VertexShaderSource = """
        #version 330 core
        layout (location = 0) in vec2 aPos;
        layout (location = 1) in vec2 aTex;
        layout (location = 2) in vec4 aColor;
        uniform mat4 uMvp;
        out vec2 vTex;
        out vec4 vColor;
        void main()
        {
            gl_Position = uMvp * vec4(aPos, 0.0, 1.0);
            vTex = aTex;
            vColor = aColor;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        in vec2 vTex;
        in vec4 vColor;
        uniform sampler2D uTexture;
        uniform float uUseTexture;
        out vec4 FragColor;
        void main()
        {
            vec4 base = uUseTexture > 0.5 ? texture(uTexture, vTex) : vec4(1.0);
            FragColor = base * vColor;
        }
        """;

    private readonly GL _gl;
    private readonly Dictionary<int, uint> _gpuTextures = new();
    private readonly List<SpriteVertex> _batch = new(4096);
    private readonly Rgba32[] _uploadScratch = new Rgba32[1024 * 1024];
    private TwoDTextureRegistry? _textures;
    private uint _program;
    private uint _vao;
    private uint _vbo;
    private int _viewportWidth;
    private int _viewportHeight;
    private bool _initialized;
    private bool _disposed;

    /// <summary>Creates a renderer for an existing OpenGL context.</summary>
    /// <param name="gl">Active GL API.</param>
    public SilkTwoDRenderer(GL gl) => _gl = gl;

    /// <inheritdoc />
    public void Resize(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
    }

    /// <inheritdoc />
    public void DrawScene(TwoDScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        EnsureInitialized();
        _textures = scene.Textures;
        scene.Camera.ViewportWidth = _viewportWidth;
        scene.Camera.ViewportHeight = _viewportHeight;

        var clear = scene.Camera.ClearColor;
        _gl.Viewport(0, 0, (uint)_viewportWidth, (uint)_viewportHeight);
        _gl.ClearColor(clear.R / 255f, clear.G / 255f, clear.B / 255f, clear.A / 255f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var worldMvp = scene.Camera.GetViewProjectionMatrix();
        DrawSprites(scene.Sprites, worldMvp);
        DrawAnimated(scene.AnimatedSprites, worldMvp);
        DrawStaticPolygons(scene, worldMvp);

        var screenMvp = Matrix4x4.CreateOrthographicOffCenter(0, _viewportWidth, _viewportHeight, 0, -1f, 1f);
        DrawHud(scene, screenMvp);
        DrawMenu(scene, screenMvp);
        _textures = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_initialized)
        {
            return;
        }

        foreach (var tex in _gpuTextures.Values)
        {
            _gl.DeleteTexture(tex);
        }

        _gpuTextures.Clear();
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteProgram(_program);
    }

    private void DrawSprites(IEnumerable<TwoDSpriteInstance> sprites, Matrix4x4 mvp)
    {
        foreach (var sprite in sprites.OrderBy(s => s.Layer).ThenBy(s => s.SortKey))
        {
            if (sprite.ScreenSpace)
            {
                continue;
            }

            EmitTexturedQuad(sprite.Texture, sprite.SourceRect, sprite.Transform, sprite.Tint, mvp);
        }
    }

    private void DrawAnimated(IEnumerable<TwoDAnimatedSprite> sprites, Matrix4x4 mvp)
    {
        foreach (var anim in sprites.OrderBy(a => a.Layer).ThenBy(a => a.SortKey))
        {
            var rect = anim.Clip.GetSourceRect(anim.CurrentFrameIndex);
            EmitTexturedQuad(anim.Clip.Sheet.Texture, rect, anim.Transform, Rgba32.White, mvp);
        }
    }

    private void DrawStaticPolygons(TwoDScene scene, Matrix4x4 mvp)
    {
        foreach (var poly in scene.StaticPolygons.OrderBy(p => p.Layer).ThenBy(p => p.SortKey))
        {
            if (poly.DrawFilled)
            {
                foreach (var face in poly.Shape.FacesSpan)
                {
                    EmitColoredTriangle(face.A, face.B, face.C, poly.FillColor, mvp);
                }
            }

            if (poly.DrawOutline)
            {
                foreach (var edge in poly.Shape.EdgesSpan)
                {
                    EmitColoredLine(edge.A, edge.B, 0.05f, poly.OutlineColor, mvp);
                }
            }
        }
    }

    private void DrawHud(TwoDScene scene, Matrix4x4 screenMvp)
    {
        foreach (var element in scene.Hud.Elements)
        {
            switch (element)
            {
                case TwoDHudText text:
                    _batch.Clear();
                    TwoDBitmapFont.EmitText(_batch, text.Text, text.ScreenX, text.ScreenY, text.Scale, text.Color);
                    Flush(screenMvp, useTexture: false, textureId: 0);
                    break;
                case TwoDHudSprite icon:
                    EmitScreenTexturedQuad(icon.Texture, icon.Source, icon.ScreenX, icon.ScreenY, icon.Width, icon.Height, icon.Tint, screenMvp);
                    break;
            }
        }
    }

    private void DrawMenu(TwoDScene scene, Matrix4x4 screenMvp)
    {
        if (scene.Menus.Active is not { } menu)
        {
            return;
        }

        if (menu.DimBackground)
        {
            EmitScreenSolidQuad(0, 0, _viewportWidth, _viewportHeight, new Rgba32(0, 0, 0, 160), screenMvp);
        }

        var titleY = _viewportHeight * 0.25f;
        EmitScreenText(menu.Title, _viewportWidth * 0.5f - menu.Title.Length * 6f, titleY, 3f, Rgba32.White, screenMvp);
        var itemY = titleY + 80f;
        for (var i = 0; i < menu.Items.Count; i++)
        {
            var prefix = i == menu.FocusIndex ? "> " : "  ";
            var color = i == menu.FocusIndex ? Rgba32.Chartreuse : Rgba32.White;
            EmitScreenText(prefix + menu.Items[i].Label, _viewportWidth * 0.5f - 80f, itemY + i * 32f, 2.5f, color, screenMvp);
        }
    }

    private void EmitTexturedQuad(
        TwoDTextureId texture,
        TwoDSourceRect source,
        TwoDTransform transform,
        Rgba32 tint,
        Matrix4x4 mvp)
    {
        if (!texture.IsValid || _textures is null)
        {
            return;
        }

        _batch.Clear();
        AddWorldQuad(_batch, transform, source, tint, mvp);
        var gpu = EnsureGpuTexture(texture);
        Flush(mvp, useTexture: true, gpu);
    }

    private void EmitScreenTexturedQuad(
        TwoDTextureId texture,
        TwoDSourceRect source,
        float x,
        float y,
        float width,
        float height,
        Rgba32 tint,
        Matrix4x4 screenMvp)
    {
        if (!texture.IsValid)
        {
            return;
        }

        _batch.Clear();
        AddScreenTexturedQuad(_batch, x, y, width, height, source, tint);
        var gpu = EnsureGpuTexture(texture);
        Flush(screenMvp, useTexture: true, gpu);
    }

    private void EmitScreenSolidQuad(float x, float y, float w, float h, Rgba32 color, Matrix4x4 screenMvp)
    {
        _batch.Clear();
        AddScreenSolidQuad(_batch, x, y, w, h, color);
        Flush(screenMvp, useTexture: false, textureId: 0);
    }

    private void EmitScreenText(string text, float x, float y, float scale, Rgba32 color, Matrix4x4 screenMvp)
    {
        _batch.Clear();
        TwoDBitmapFont.EmitText(_batch, text, x, y, scale, color);
        Flush(screenMvp, useTexture: false, textureId: 0);
    }

    private void EmitColoredTriangle(Vector3 a, Vector3 b, Vector3 c, Rgba32 color, Matrix4x4 mvp)
    {
        _batch.Clear();
        var r = color.R / 255f;
        var g = color.G / 255f;
        var bl = color.B / 255f;
        var al = color.A / 255f;
        AddWorldVertex(_batch, a, 0f, 0f, r, g, bl, al);
        AddWorldVertex(_batch, b, 0f, 0f, r, g, bl, al);
        AddWorldVertex(_batch, c, 0f, 0f, r, g, bl, al);
        Flush(mvp, useTexture: false, textureId: 0);
    }

    private void EmitColoredLine(Vector3 a, Vector3 b, float thickness, Rgba32 color, Matrix4x4 mvp)
    {
        var dir = b - a;
        dir.Y = 0f;
        if (dir.LengthSquared() < 1e-8f)
        {
            return;
        }

        dir = Vector3.Normalize(dir);
        var perp = new Vector3(-dir.Z, 0f, dir.X) * thickness * 0.5f;
        EmitColoredTriangle(a + perp, b + perp, b - perp, color, mvp);
        EmitColoredTriangle(a + perp, b - perp, a - perp, color, mvp);
    }

    private static void AddWorldQuad(
        List<SpriteVertex> batch,
        TwoDTransform transform,
        TwoDSourceRect source,
        Rgba32 tint,
        Matrix4x4 mvp)
    {
        var w = transform.Scale.X;
        var h = transform.Scale.Z;
        var cx = transform.Position.X;
        var cz = transform.Position.Z;
        var u0 = source.U0;
        var v0 = source.V0;
        var u1 = source.U1;
        var v1 = source.V1;
        if (transform.FlipX)
        {
            (u0, u1) = (u1, u0);
        }

        var r = tint.R / 255f;
        var g = tint.G / 255f;
        var b = tint.B / 255f;
        var a = tint.A / 255f;
        var corners = new[]
        {
            new Vector3(-w * 0.5f, 0f, -h * 0.5f),
            new Vector3(w * 0.5f, 0f, -h * 0.5f),
            new Vector3(-w * 0.5f, 0f, h * 0.5f),
            new Vector3(w * 0.5f, 0f, h * 0.5f),
        };
        var rot = Matrix4x4.CreateRotationY(transform.RotationY);
        for (var i = 0; i < corners.Length; i++)
        {
            var local = Vector3.Transform(corners[i], rot);
            corners[i] = new Vector3(local.X + cx, 0f, local.Z + cz);
        }

        AddQuad(
            batch,
            new Vector2(corners[0].X, corners[0].Z),
            new Vector2(corners[1].X, corners[1].Z),
            new Vector2(corners[2].X, corners[2].Z),
            new Vector2(corners[3].X, corners[3].Z),
            u0,
            v1,
            u1,
            v1,
            u0,
            v0,
            u1,
            v0,
            r,
            g,
            b,
            a);
    }

    private static void AddScreenTexturedQuad(
        List<SpriteVertex> batch,
        float x,
        float y,
        float width,
        float height,
        TwoDSourceRect source,
        Rgba32 tint)
    {
        var r = tint.R / 255f;
        var g = tint.G / 255f;
        var b = tint.B / 255f;
        var a = tint.A / 255f;
        AddQuad(
            batch,
            new Vector2(x, y),
            new Vector2(x + width, y),
            new Vector2(x, y + height),
            new Vector2(x + width, y + height),
            source.U0,
            source.V1,
            source.U1,
            source.V1,
            source.U0,
            source.V0,
            source.U1,
            source.V0,
            r,
            g,
            b,
            a);
    }

    private static void AddScreenSolidQuad(List<SpriteVertex> batch, float x, float y, float w, float h, Rgba32 color)
    {
        var r = color.R / 255f;
        var g = color.G / 255f;
        var b = color.B / 255f;
        var a = color.A / 255f;
        AddQuad(
            batch,
            new Vector2(x, y),
            new Vector2(x + w, y),
            new Vector2(x, y + h),
            new Vector2(x + w, y + h),
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            r,
            g,
            b,
            a);
    }

    private static void AddQuad(
        List<SpriteVertex> batch,
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        float u0,
        float v0,
        float u1,
        float v1,
        float u2,
        float v2,
        float u3,
        float v3,
        float r,
        float g,
        float b,
        float a)
    {
        batch.Add(new SpriteVertex(p0.X, p0.Y, u0, v0, r, g, b, a));
        batch.Add(new SpriteVertex(p1.X, p1.Y, u1, v1, r, g, b, a));
        batch.Add(new SpriteVertex(p2.X, p2.Y, u2, v2, r, g, b, a));
        batch.Add(new SpriteVertex(p0.X, p0.Y, u0, v0, r, g, b, a));
        batch.Add(new SpriteVertex(p2.X, p2.Y, u2, v2, r, g, b, a));
        batch.Add(new SpriteVertex(p3.X, p3.Y, u3, v3, r, g, b, a));
    }

    private static void AddWorldVertex(List<SpriteVertex> batch, Vector3 world, float u, float v, float r, float g, float b, float a) =>
        batch.Add(new SpriteVertex(world.X, world.Z, u, v, r, g, b, a));

    private void Flush(Matrix4x4 mvp, bool useTexture, uint textureId)
    {
        if (_batch.Count == 0)
        {
            return;
        }

        _gl.UseProgram(_program);
        UploadMvp(mvp);
        _gl.Uniform1(_gl.GetUniformLocation(_program, "uUseTexture"), useTexture ? 1f : 0f);
        if (useTexture && textureId != 0)
        {
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, textureId);
            _gl.Uniform1(_gl.GetUniformLocation(_program, "uTexture"), 0);
        }

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (SpriteVertex* ptr = CollectionsMarshal.AsSpan(_batch))
            {
                _gl.BufferData(
                    BufferTargetARB.ArrayBuffer,
                    (nuint)(_batch.Count * sizeof(SpriteVertex)),
                    ptr,
                    BufferUsageARB.StreamDraw);
            }
        }

        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_batch.Count);
        _gl.BindVertexArray(0);
        _batch.Clear();
    }

    private uint EnsureGpuTexture(TwoDTextureId id)
    {
        if (_gpuTextures.TryGetValue(id.Value, out var existing))
        {
            return existing;
        }

        if (_textures is null || !_textures.Contains(id))
        {
            return 0;
        }

        var info = _textures.GetInfo(id);
        var pixelCount = info.Width * info.Height;
        if (pixelCount > _uploadScratch.Length)
        {
            throw new InvalidOperationException("Texture exceeds upload scratch buffer.");
        }

        _textures.CopyPixels(id, _uploadScratch.AsSpan(0, pixelCount), out var w, out var h);
        var glTex = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, glTex);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        unsafe
        {
            fixed (Rgba32* ptr = _uploadScratch)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)w,
                    (uint)h,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr);
            }
        }

        _gpuTextures[id.Value] = glTex;
        return glTex;
    }

    private void UploadMvp(Matrix4x4 mvp)
    {
        unsafe
        {
            var loc = _gl.GetUniformLocation(_program, "uMvp");
            _gl.UniformMatrix4(loc, 1, false, (float*)&mvp);
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _program = CompileProgram();
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        var stride = 8 * sizeof(float);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)stride, 0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)stride, 2 * sizeof(float));
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)stride, 4 * sizeof(float));
        _gl.BindVertexArray(0);
        _initialized = true;
    }

    private uint CompileProgram()
    {
        var vertex = CompileShader(ShaderType.VertexShader, VertexShaderSource);
        var fragment = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);
        var program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);
        _gl.LinkProgram(program);
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
        if (status == 0)
        {
            throw new InvalidOperationException($"GL link failed: {_gl.GetProgramInfoLog(program)}");
        }

        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
        if (status == 0)
        {
            throw new InvalidOperationException($"GL compile failed: {_gl.GetShaderInfoLog(shader)}");
        }

        return shader;
    }
}
