using Novolis.Rendering.Runtime;

namespace Novolis.Rendering.PathTrace.Demos;

/// <summary>Formats window title strings for path-tracing demos (Silk HUD via title bar).</summary>
public static class PathTraceStatusTitle
{
    /// <summary>Builds a single-line status title for Silk and Raylib samples.</summary>
    /// <param name="backend">Active backend.</param>
    /// <param name="sampleCount">Displayed sample count.</param>
    /// <param name="orbitEnabled">Whether auto-orbit mode is active.</param>
    /// <param name="fps">Smoothed frames per second (optional).</param>
    /// <param name="rendering">Whether a background trace is running.</param>
    /// <param name="extra">Optional suffix (controls hint).</param>
    /// <returns>Title bar text.</returns>
    public static string Format(
        IRayTracingBackend backend,
        int sampleCount,
        bool orbitEnabled,
        float? fps = null,
        bool rendering = false,
        string? extra = null)
    {
        var samples = rendering && sampleCount == 0 ? "…" : sampleCount.ToString();
        var orbit = orbitEnabled ? "on" : "off";
        var rayBackend = Environment.GetEnvironmentVariable("NOVOLIS_RAY_BACKEND") ?? "(default ilgpu)";
        var ilgpuDevice = Environment.GetEnvironmentVariable("NOVOLIS_ILGPU_DEVICE") ?? "(auto)";
        var fpsPart = fps is > 0f ? $" | {fps.Value:F0} fps" : string.Empty;
        var extraPart = string.IsNullOrEmpty(extra) ? string.Empty : $" | {extra}";
        return $"{backend.BackendLabel} | samples {samples} | orbit {orbit}{fpsPart} | RAY_BACKEND={rayBackend} ILGPU={ilgpuDevice}{extraPart}";
    }
}
