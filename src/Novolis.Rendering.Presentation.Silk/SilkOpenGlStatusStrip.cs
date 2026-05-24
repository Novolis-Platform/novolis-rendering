using Silk.NET.OpenGL;

namespace Novolis.Rendering.Presentation.Silk;

/// <summary>Draws a semi-transparent status strip at the top of the framebuffer (Silk HUD gap-fill).</summary>
public static class SilkOpenGlStatusStrip
{
    private const string VertexShaderSource = """
        #version 330 core
        layout (location = 0) in vec2 aPos;
        void main()
        {
            gl_Position = vec4(aPos, 0.0, 1.0);
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        out vec4 FragColor;
        uniform vec4 uColor;
        void main()
        {
            FragColor = uColor;
        }
        """;

    private static uint _program;
    private static uint _vao;
    private static uint _vbo;
    private static bool _initialized;

    /// <summary>Draws a top bar overlay in normalized device coordinates.</summary>
    /// <param name="gl">Active OpenGL context.</param>
    /// <param name="viewportHeight">Framebuffer height in pixels.</param>
    /// <param name="stripHeightPixels">Strip height in pixels.</param>
    /// <param name="alpha">Opacity (0–1).</param>
    public static void Draw(GL gl, int viewportHeight, int stripHeightPixels = 112, float alpha = 0.55f)
    {
        if (viewportHeight <= 0)
        {
            return;
        }

        EnsureInitialized(gl);
        var topNdc = 1f - 2f * stripHeightPixels / viewportHeight;
        ReadOnlySpan<float> vertices =
        [
            -1f, 1f,
            1f, 1f,
            -1f, topNdc,
            1f, topNdc,
        ];

        gl.UseProgram(_program);
        gl.Uniform4(gl.GetUniformLocation(_program, "uColor"), 0f, 0f, 0f, alpha);
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.DynamicDraw);
        gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        gl.BindVertexArray(0);
    }

    private static void EnsureInitialized(GL gl)
    {
        if (_initialized)
        {
            return;
        }

        _program = CompileProgram(gl);
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        gl.BindVertexArray(0);
        _initialized = true;
    }

    private static uint CompileProgram(GL gl)
    {
        var vertex = CompileShader(gl, ShaderType.VertexShader, VertexShaderSource);
        var fragment = CompileShader(gl, ShaderType.FragmentShader, FragmentShaderSource);
        var program = gl.CreateProgram();
        gl.AttachShader(program, vertex);
        gl.AttachShader(program, fragment);
        gl.LinkProgram(program);
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
            throw new InvalidOperationException($"GL shader compile failed: {gl.GetShaderInfoLog(shader)}");
        }

        return shader;
    }
}
