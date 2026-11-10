namespace Novolis.Rendering.Runtime;

/// <summary>Optional bounce-budget control for interactive path-trace backends.</summary>
public interface ITraceDepthControl
{
    /// <summary>Maximum path depth for subsequent samples (clamped by the backend).</summary>
    int MaxTraceDepth { get; set; }
}
