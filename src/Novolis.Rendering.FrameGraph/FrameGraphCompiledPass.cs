namespace Novolis.Rendering.FrameGraph;

/// <summary>One pass in deterministic execution order.</summary>
public sealed class FrameGraphCompiledPass
{
    internal FrameGraphCompiledPass(
        FrameGraphPassDescription description,
        int executionIndex,
        IReadOnlyList<FrameGraphPassHandle> dependencies,
        IReadOnlyList<FrameGraphBarrier> barriers)
    {
        Description = description;
        ExecutionIndex = executionIndex;
        Dependencies = dependencies;
        Barriers = barriers;
    }

    /// <summary>Original pass declaration.</summary>
    public FrameGraphPassDescription Description { get; }

    /// <summary>Zero-based execution order.</summary>
    public int ExecutionIndex { get; }

    /// <summary>Passes that must complete before this pass.</summary>
    public IReadOnlyList<FrameGraphPassHandle> Dependencies { get; }

    /// <summary>Barriers emitted immediately before this pass.</summary>
    public IReadOnlyList<FrameGraphBarrier> Barriers { get; }
}
