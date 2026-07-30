using System.Reflection;
using Silk.NET.Shaderc;

namespace Novolis.Rendering.Backends.Vulkan;

internal static unsafe class VulkanShaderCompiler
{
    private static byte[]? _cachedPathTraceSpirv;
    private static byte[]? _cachedWireVertSpirv;
    private static byte[]? _cachedWireFragSpirv;

    public static ReadOnlySpan<byte> GetPathTraceSpirv()
    {
        _cachedPathTraceSpirv ??= Compile(
            LoadEmbeddedShader("Novolis.Rendering.Backends.Vulkan.Shaders.path_trace.comp"),
            ShaderKind.ComputeShader,
            "path_trace.comp");
        return _cachedPathTraceSpirv;
    }

    public static ReadOnlySpan<byte> GetWireVertexSpirv()
    {
        _cachedWireVertSpirv ??= Compile(
            LoadEmbeddedShader("Novolis.Rendering.Backends.Vulkan.Shaders.wire.vert"),
            ShaderKind.VertexShader,
            "wire.vert");
        return _cachedWireVertSpirv;
    }

    public static ReadOnlySpan<byte> GetWireFragmentSpirv()
    {
        _cachedWireFragSpirv ??= Compile(
            LoadEmbeddedShader("Novolis.Rendering.Backends.Vulkan.Shaders.wire.frag"),
            ShaderKind.FragmentShader,
            "wire.frag");
        return _cachedWireFragSpirv;
    }

    private static string LoadEmbeddedShader(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded shader resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static byte[] Compile(string source, ShaderKind kind, string fileName)
    {
        var shaderc = Shaderc.GetApi();
        var compiler = shaderc.CompilerInitialize();
        if (compiler == null)
            throw new InvalidOperationException("shaderc compiler init failed.");

        try
        {
            var options = shaderc.CompileOptionsInitialize();
            if (options == null)
                throw new InvalidOperationException("shaderc options init failed.");

            try
            {
                shaderc.CompileOptionsSetTargetEnv(options, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan11);
                var result = shaderc.CompileIntoSpv(
                    compiler,
                    source,
                    (nuint)source.Length,
                    kind,
                    fileName,
                    "main",
                    options);

                if (result == null)
                    throw new InvalidOperationException("shaderc returned null result.");

                try
                {
                    var status = shaderc.ResultGetCompilationStatus(result);
                    if (status != CompilationStatus.Success)
                    {
                        var message = shaderc.ResultGetErrorMessageS(result) ?? "unknown shader compile error";
                        throw new InvalidOperationException($"Vulkan shader compile failed ({fileName}): {message}");
                    }

                    var bytes = shaderc.ResultGetBytes(result);
                    var length = shaderc.ResultGetLength(result);
                    var spirv = new byte[length];
                    fixed (byte* dst = spirv)
                        System.Buffer.MemoryCopy(bytes, dst, length, length);
                    return spirv;
                }
                finally
                {
                    shaderc.ResultRelease(result);
                }
            }
            finally
            {
                shaderc.CompileOptionsRelease(options);
            }
        }
        finally
        {
            shaderc.CompilerRelease(compiler);
        }
    }
}
