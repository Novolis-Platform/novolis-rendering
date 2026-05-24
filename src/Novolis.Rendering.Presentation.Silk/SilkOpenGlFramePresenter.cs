using Novolis.Math.Geometry;
using Novolis.Rendering.Presentation.Abstractions;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Novolis.Rendering.Presentation.Silk;

/// <summary>Uploads CPU RGBA frames to an OpenGL texture and draws a full-screen quad.</summary>
public sealed class SilkOpenGlFramePresenter : IFramePresenter, IDisposable
{
    private const string VertexShaderSource = """
        #version 330 core
        layout (location = 0) in vec2 aPos;
        layout (location = 1) in vec2 aTex;
        out vec2 vTex;
        void main()
        {
            gl_Position = vec4(aPos, 0.0, 1.0);
            vTex = aTex;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        in vec2 vTex;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        void main()
        {
            FragColor = texture(uTexture, vTex);
        }
        """;

    private readonly IWindow _window;
    private readonly object _frameLock = new();
    private GL? _gl;
    private uint _program;
    private uint _vao;
    private uint _vbo;
    private uint _texture;
    private int _textureWidth;
    private int _textureHeight;
    private byte[]? _pending;
    private int _pendingWidth;
    private int _pendingHeight;
    private bool _hasPending;
    private bool _initialized;
    private bool _disposed;

    /// <summary>When true, draws a semi-transparent strip at the top before the frame quad (HUD area).</summary>
    public bool ShowStatusStrip { get; set; }

    /// <summary>Creates a presenter for the given Silk window.</summary>
    /// <param name="window">Target window (OpenGL context).</param>
    public SilkOpenGlFramePresenter(IWindow window) => _window = window;

    /// <inheritdoc />
    public void PresentCpuFrame(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        var byteCount = width * height * 4;
        lock (_frameLock)
        {
            if (_pending is null || _pending.Length < byteCount)
            {
                _pending = new byte[byteCount];
            }

            var idx = 0;
            for (var y = 0; y < height; y++)
            {
                var row = y * width;
                for (var x = 0; x < width; x++)
                {
                    var p = pixels[row + x];
                    _pending[idx++] = p.R;
                    _pending[idx++] = p.G;
                    _pending[idx++] = p.B;
                    _pending[idx++] = p.A;
                }
            }

            _pendingWidth = width;
            _pendingHeight = height;
            _hasPending = true;
        }
    }

    /// <summary>Draws the latest frame; call from the window <c>Render</c> callback.</summary>
    public void Draw()
    {
        if (_disposed || _gl is null || !_initialized)
        {
            return;
        }

        byte[]? upload;
        int width;
        int height;
        lock (_frameLock)
        {
            if (!_hasPending || _pending is null)
            {
                if (_texture == 0)
                {
                    return;
                }

                DrawQuad();
                return;
            }

            upload = _pending;
            width = _pendingWidth;
            height = _pendingHeight;
            _hasPending = false;
        }

        EnsureTexture(width, height);
        _gl.TexImage2D(
            TextureTarget.Texture2D,
            0,
            InternalFormat.Rgba,
            (uint)width,
            (uint)height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            upload);

        DrawQuad();
    }

    /// <summary>Releases GL objects created by <see cref="Initialize"/>.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_gl is null || !_initialized)
        {
            return;
        }

        _gl.DeleteTexture(_texture);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteProgram(_program);
    }

    internal void Initialize(GL gl)
    {
        if (_initialized)
        {
            return;
        }

        _gl = gl;
        _program = CompileProgram(gl);
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        ReadOnlySpan<float> vertices =
        [
            -1f, -1f, 0f, 0f,
            1f, -1f, 1f, 0f,
            -1f, 1f, 0f, 1f,
            1f, 1f, 1f, 1f,
        ];
        gl.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        gl.BindVertexArray(0);
        _initialized = true;
    }

    private void EnsureTexture(int width, int height)
    {
        if (_gl is null)
        {
            return;
        }

        if (_texture != 0 && _textureWidth == width && _textureHeight == height)
        {
            return;
        }

        if (_texture != 0)
        {
            _gl.DeleteTexture(_texture);
        }

        _textureWidth = width;
        _textureHeight = height;
        _texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
    }

    private void DrawQuad()
    {
        if (_gl is null || _texture == 0)
        {
            return;
        }

        var viewportHeight = _window.FramebufferSize.Y;
        _gl.Viewport(0, 0, (uint)_window.FramebufferSize.X, (uint)viewportHeight);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        if (ShowStatusStrip)
        {
            SilkOpenGlStatusStrip.Draw(_gl, viewportHeight);
        }

        _gl.UseProgram(_program);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
        _gl.Uniform1(_gl.GetUniformLocation(_program, "uTexture"), 0);
        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        _gl.BindVertexArray(0);
    }

    private static uint CompileProgram(GL gl)
    {
        var vertex = CompileShader(gl, ShaderType.VertexShader, VertexShaderSource);
        var fragment = CompileShader(gl, ShaderType.FragmentShader, FragmentShaderSource);
        var program = gl.CreateProgram();
        gl.AttachShader(program, vertex);
        gl.AttachShader(program, fragment);
        gl.LinkProgram(program);
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
        if (status == 0)
        {
            var log = gl.GetProgramInfoLog(program);
            throw new InvalidOperationException($"GL program link failed: {log}");
        }

        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);
        return program;
    }

    private static uint CompileShader(GL gl, ShaderType type, string source)
    {
        var shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
        if (status == 0)
        {
            var log = gl.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"GL shader compile failed: {log}");
        }

        return shader;
    }
}
