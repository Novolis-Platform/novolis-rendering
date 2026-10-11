namespace Novolis.Rendering.FrameGraph;

/// <summary>
/// Deterministic CPU bake/reference contract for graph validation and capture.
/// </summary>
/// <remarks>
/// This target intentionally does not claim to execute shading or image baking.
/// The existing CPU ray-tracing backend remains the execution authority for
/// path-traced pixels; this type validates the frame contract around it.
/// </remarks>
public sealed class CpuBakeFrameGraphTarget : IFrameGraphTarget
{
    private readonly CpuReferenceFrameGraphTarget _reference;

    /// <summary>Creates a CPU bake/reference target.</summary>
    public CpuBakeFrameGraphTarget(FrameGraphSize size)
    {
        _reference = new CpuReferenceFrameGraphTarget(size, FrameGraphTargetCapabilities.CpuBake);
    }

    /// <summary>Target identifier for diagnostics.</summary>
    public string TargetId => "cpu-bake-reference";

    /// <summary>CPU bake/reference capability declaration.</summary>
    public FrameGraphTargetCapabilities Capabilities => _reference.Capabilities;

    /// <summary>Typed target description.</summary>
    public FrameGraphTargetDescription TargetDescription { get; } =
        FrameGraphTargetDescription.CpuBake;

    /// <summary>Current validation size.</summary>
    public FrameGraphSize Size => _reference.Size;

    /// <summary>Most recent accepted graph validation.</summary>
    public FrameGraphTargetExecutionResult? LastExecution => _reference.LastExecution;

    /// <inheritdoc />
    public void Resize(FrameGraphSize size) => _reference.Resize(size);

    /// <inheritdoc />
    public FrameGraphTargetExecutionResult Execute(CompiledFrameGraph graph) =>
        _reference.Execute(graph);
}
