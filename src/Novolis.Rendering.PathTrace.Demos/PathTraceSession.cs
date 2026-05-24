using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>Owns a ray-tracing backend, scene upload, and resize lifecycle for demo apps.</summary>
public sealed class PathTraceSession : IDisposable
{
    private readonly CompiledScene _scene;
    private IRayTracingBackend _backend;
    private PathTraceBackendKind _kind;
    private int _width;
    private int _height;

    /// <summary>Creates a session with the backend from <see cref="PathTraceBackendFactory.FromEnvironment"/>.</summary>
    /// <param name="scene">Compiled scene to upload on resize.</param>
    public PathTraceSession(CompiledScene scene)
        : this(scene, PathTraceBackendFactory.FromEnvironment())
    {
    }

    /// <summary>Creates a session with an explicit backend kind.</summary>
    /// <param name="scene">Compiled scene to upload on resize.</param>
    /// <param name="initialKind">Initial backend.</param>
    public PathTraceSession(CompiledScene scene, PathTraceBackendKind initialKind)
    {
        _scene = scene;
        _kind = initialKind;
        _backend = PathTraceBackendFactory.Create(initialKind);
    }

    /// <summary>Active backend instance.</summary>
    public IRayTracingBackend Backend => _backend;

    /// <summary>Active backend kind.</summary>
    public PathTraceBackendKind BackendKind => _kind;

    /// <summary>Resizes buffers and re-uploads the compiled scene.</summary>
    /// <param name="width">Framebuffer width.</param>
    /// <param name="height">Framebuffer height.</param>
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _backend.ResizeAsync(width, height).GetAwaiter().GetResult();
        _backend.UploadSceneAsync(_scene).GetAwaiter().GetResult();
    }

    /// <summary>Replaces the backend (e.g. hot-key switch in studio demos).</summary>
    /// <param name="kind">New backend kind.</param>
    public void SwitchBackend(PathTraceBackendKind kind)
    {
        if (kind == _kind)
        {
            return;
        }

        DisposeBackend(_backend);
        _kind = kind;
        _backend = PathTraceBackendFactory.Create(kind);
        if (_width > 0 && _height > 0)
        {
            Resize(_width, _height);
        }
    }

    /// <summary>Cycles cpu → ilgpu → vulkan.</summary>
    /// <returns>The new backend kind.</returns>
    public PathTraceBackendKind CycleBackend()
    {
        var next = _kind switch
        {
            PathTraceBackendKind.Cpu => PathTraceBackendKind.Ilgpu,
            PathTraceBackendKind.Ilgpu => PathTraceBackendKind.Vulkan,
            _ => PathTraceBackendKind.Cpu,
        };
        SwitchBackend(next);
        return next;
    }

    /// <inheritdoc />
    public void Dispose() => DisposeBackend(_backend);

    private static void DisposeBackend(IRayTracingBackend backend)
    {
        if (backend is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
