using System.Reflection;
using Silk.NET.Shaderc;

namespace Novolis.Rendering.Backends.Vulkan;

internal static unsafe class VulkanShaderCompiler
{
    private static byte[]? _cachedSpirv;

    public static ReadOnlySpan<byte> GetPathTraceSpirv()
    {
        if (_cachedSpirv is not null)
        {
            return _cachedSpirv;
        }

        var source = LoadEmbeddedShader("Novolis.Rendering.Backends.Vulkan.Shaders.path_trace.comp");
        _cachedSpirv = CompileCompute(source);
        return _cachedSpirv;
    }

    private static string LoadEmbeddedShader(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded shader resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static byte[] CompileCompute(string source)
    {
        var shaderc = Shaderc.GetApi();
        var compiler = shaderc.CompilerInitialize();
        if (compiler == null)
        {
            throw new InvalidOperationException("shaderc compiler init failed.");
        }

        try
        {
            var options = shaderc.CompileOptionsInitialize();
            if (options == null)
            {
                throw new InvalidOperationException("shaderc options init failed.");
            }

            try
            {
                shaderc.CompileOptionsSetTargetEnv(options, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan11);
                var result = shaderc.CompileIntoSpv(
                    compiler,
                    source,
                    (nuint)source.Length,
                    ShaderKind.ComputeShader,
                    "path_trace.comp",
                    "main",
                    options);

                if (result == null)
                {
                    throw new InvalidOperationException("shaderc returned null result.");
                }

                try
                {
                    var status = shaderc.ResultGetCompilationStatus(result);
                    if (status != CompilationStatus.Success)
                    {
                        var message = shaderc.ResultGetErrorMessageS(result) ?? "unknown shader compile error";
                        throw new InvalidOperationException($"Vulkan shader compile failed: {message}");
                    }

                    var bytes = shaderc.ResultGetBytes(result);
                    var length = shaderc.ResultGetLength(result);
                    var spirv = new byte[length];
                    unsafe
                    {
                        fixed (byte* dst = spirv)
                        {
                            System.Buffer.MemoryCopy(bytes, dst, length, length);
                        }
                    }

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
