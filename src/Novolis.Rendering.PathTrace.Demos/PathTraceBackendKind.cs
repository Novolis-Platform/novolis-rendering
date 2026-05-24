namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>Selectable path-tracing backends for interactive demos.</summary>
public enum PathTraceBackendKind
{
    /// <summary>ILGPU compute (CUDA/OpenCL/CPU fallback).</summary>
    Ilgpu,

    /// <summary>Vulkan compute (SPIR-V).</summary>
    Vulkan,

    /// <summary>CPU reference tracer.</summary>
    Cpu,
}
