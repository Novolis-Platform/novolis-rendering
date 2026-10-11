namespace Novolis.Rendering.FrameGraph;

/// <summary>Renderer-neutral target contract for a compiled frame graph.</summary>
public interface IFrameGraphTarget
{
    /// <summary>Target identifier for logs and diagnostics.</summary>
    string TargetId { get; }

    /// <summary>Target feature declaration.</summary>
    FrameGraphTargetCapabilities Capabilities { get; }

    /// <summary>Current output size.</summary>
    FrameGraphSize Size { get; }

    /// <summary>Resizes target-owned storage or validation state.</summary>
    void Resize(FrameGraphSize size);

    /// <summary>Validates and records one compiled graph execution.</summary>
    FrameGraphTargetExecutionResult Execute(CompiledFrameGraph graph);
}
