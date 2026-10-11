namespace Novolis.Rendering.FrameGraph;

/// <summary>State tracked for a resource between adjacent graph accesses.</summary>
public enum FrameGraphResourceState
{
    /// <summary>No prior state is known.</summary>
    Undefined,
    /// <summary>State supplied by an external owner.</summary>
    External,
    /// <summary>Shader read state.</summary>
    ShaderRead,
    /// <summary>Shader write state.</summary>
    ShaderWrite,
    /// <summary>Color attachment state.</summary>
    ColorAttachment,
    /// <summary>Read-only depth state.</summary>
    DepthRead,
    /// <summary>Writable depth state.</summary>
    DepthWrite,
    /// <summary>Transfer source state.</summary>
    TransferRead,
    /// <summary>Transfer destination state.</summary>
    TransferWrite,
    /// <summary>Vertex input state.</summary>
    VertexInput,
    /// <summary>Index input state.</summary>
    IndexInput,
    /// <summary>Uniform input state.</summary>
    UniformRead,
    /// <summary>Presentation state.</summary>
    Present,
}
