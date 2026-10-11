namespace Novolis.Rendering.FrameGraph;

/// <summary>Small CPU/reference target for graph validation and deterministic capture.</summary>
public sealed class CpuReferenceFrameGraphTarget : IFrameGraphTarget
{
    private FrameGraphSize _size;

    /// <summary>Creates a one-pixel CPU reference target.</summary>
    public CpuReferenceFrameGraphTarget()
        : this(new FrameGraphSize(1, 1))
    {
    }

    /// <summary>Creates a CPU reference target with all declared foundation features.</summary>
    public CpuReferenceFrameGraphTarget(
        FrameGraphSize size,
        FrameGraphTargetCapabilities? capabilities = null)
    {
        _size = size;
        Capabilities = capabilities ?? FrameGraphTargetCapabilities.CpuReference;
        if (!Capabilities.SupportsSize(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "The target capability limit does not accept this size.");
        }

        TargetDescription = new(
            "cpu-reference",
            Capabilities.Name,
            Capabilities);
    }

    /// <inheritdoc />
    public string TargetId => "cpu-reference";

    /// <inheritdoc />
    public FrameGraphTargetCapabilities Capabilities { get; }

    /// <summary>Typed description of this headless validation target.</summary>
    public FrameGraphTargetDescription TargetDescription { get; }

    /// <inheritdoc />
    public FrameGraphSize Size => _size;

    /// <summary>Most recent accepted execution, if any.</summary>
    public FrameGraphTargetExecutionResult? LastExecution { get; private set; }

    /// <inheritdoc />
    public void Resize(FrameGraphSize size)
    {
        if (!Capabilities.SupportsSize(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "The target capability limit does not accept this size.");
        }

        _size = size;
    }

    /// <inheritdoc />
    public FrameGraphTargetExecutionResult Execute(CompiledFrameGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var diagnostics = new List<FrameGraphDiagnostic>();
        if (graph.OutputSize != Size)
        {
            diagnostics.Add(
                new FrameGraphDiagnostic(
                    FrameGraphDiagnosticSeverity.Error,
                    "FG300",
                    $"Graph output size {graph.OutputSize} does not match target size {Size}."));
        }

        if (!Capabilities.SupportsSize(graph.OutputSize))
        {
            diagnostics.Add(
                new FrameGraphDiagnostic(
                    FrameGraphDiagnosticSeverity.Error,
                    "FG301",
                    $"Target '{Capabilities.Name}' does not support graph output size {graph.OutputSize}."));
        }

        var validation = graph.Description.Validate(Capabilities, graph.QualityProfile);
        diagnostics.AddRange(validation.Diagnostics);
        var accepted = diagnostics.All(diagnostic => diagnostic.Severity != FrameGraphDiagnosticSeverity.Error);
        var result = new FrameGraphTargetExecutionResult(
            accepted,
            accepted ? graph.Passes.Count : 0,
            accepted ? graph.CaptureDiagnostics() : null,
            FrameGraphDescription.Freeze(diagnostics),
            TargetDescription);
        LastExecution = accepted ? result : LastExecution;
        return result;
    }
}
