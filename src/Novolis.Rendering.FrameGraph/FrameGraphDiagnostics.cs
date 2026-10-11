using System.Text;
using System.Text.Json;

namespace Novolis.Rendering.FrameGraph;

/// <summary>Deterministic, serializable view of a compiled frame graph.</summary>
public sealed class FrameGraphDiagnosticGraph
{
    internal FrameGraphDiagnosticGraph(
        string name,
        FrameGraphSize outputSize,
        FrameGraphSize renderSize,
        string qualityProfile,
        float qualityScale,
        FrameGraphTargetDescription? targetDescription,
        IReadOnlyList<FrameGraphDiagnosticResource> resources,
        IReadOnlyList<FrameGraphDiagnosticPass> passes,
        IReadOnlyList<FrameGraphDiagnosticBarrier> barriers)
    {
        Name = name;
        OutputSize = outputSize;
        RenderSize = renderSize;
        QualityProfile = qualityProfile;
        QualityScale = qualityScale;
        TargetDescription = targetDescription;
        Resources = resources;
        Passes = passes;
        Barriers = barriers;
    }

    /// <summary>Graph name.</summary>
    public string Name { get; }

    /// <summary>External output dimensions.</summary>
    public FrameGraphSize OutputSize { get; }

    /// <summary>Quality-scaled render dimensions.</summary>
    public FrameGraphSize RenderSize { get; }

    /// <summary>Quality profile name.</summary>
    public string QualityProfile { get; }

    /// <summary>Quality render scale.</summary>
    public float QualityScale { get; }

    /// <summary>Typed target description used for compilation, when supplied.</summary>
    public FrameGraphTargetDescription? TargetDescription { get; }

    /// <summary>Resources in stable resource-id order.</summary>
    public IReadOnlyList<FrameGraphDiagnosticResource> Resources { get; }

    /// <summary>Passes in stable execution order.</summary>
    public IReadOnlyList<FrameGraphDiagnosticPass> Passes { get; }

    /// <summary>Barriers in stable execution order.</summary>
    public IReadOnlyList<FrameGraphDiagnosticBarrier> Barriers { get; }

    /// <summary>Captures a compiled graph without executing any pass.</summary>
    public static FrameGraphDiagnosticGraph Capture(CompiledFrameGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var resources = graph.Resources
            .OrderBy(resource => resource.Description.Handle.Id)
            .Select(
                resource =>
                {
                    var size = resource.ResolvedSize;
                    return new FrameGraphDiagnosticResource(
                        resource.Description.Handle.Id,
                        resource.Description.Kind.ToString(),
                        resource.Description.Name,
                        resource.Description.FormatLabel,
                        size?.Width ?? 0,
                        size?.Height ?? 0,
                        resource.FirstUseIndex,
                        resource.LastUseIndex,
                        resource.Description.Lifetime.ToString());
                })
            .ToArray();
        var passes = graph.Passes
            .OrderBy(pass => pass.ExecutionIndex)
            .Select(
                pass => new FrameGraphDiagnosticPass(
                    pass.Description.Handle.Id,
                    pass.ExecutionIndex,
                    pass.Description.Name,
                    pass.Description.Kind.ToString(),
                    pass.Dependencies.Select(dependency => dependency.Id).OrderBy(id => id).ToArray(),
                    pass.Description.Accesses.Select(access => access.Resource.Id).Distinct().OrderBy(id => id).ToArray(),
                    pass.Description.RequiredFeatures.ToString())
                {
                    Stage = pass.Description.Stage.ToString(),
                })
            .ToArray();
        var barriers = graph.Barriers
            .OrderBy(barrier => barrier.ExecutionIndex)
            .ThenBy(barrier => barrier.Pass.Id)
            .ThenBy(barrier => barrier.Resource.Id)
            .Select(
                barrier => new FrameGraphDiagnosticBarrier(
                    barrier.ExecutionIndex,
                    barrier.Pass.Id,
                    barrier.Resource.Id,
                    barrier.Before.ToString(),
                    barrier.After.ToString(),
                    barrier.Access.ToString(),
                    barrier.Usage.ToString()))
            .ToArray();

        return new FrameGraphDiagnosticGraph(
            graph.Description.Name,
            graph.OutputSize,
            graph.RenderSize,
            graph.QualityProfile.Name,
            graph.QualityProfile.RenderScale,
            graph.TargetDescription,
            FrameGraphDescription.Freeze(resources),
            FrameGraphDescription.Freeze(passes),
            FrameGraphDescription.Freeze(barriers));
    }

    /// <summary>Returns a line-oriented representation with stable ordering.</summary>
    public string ToDeterministicText()
    {
        var text = new StringBuilder();
        text.Append("graph|")
            .Append(Sanitize(Name))
            .Append("|output=")
            .Append(OutputSize)
            .Append("|render=")
            .Append(RenderSize)
            .Append("|quality=")
            .Append(Sanitize(QualityProfile))
            .Append("|scale=")
            .Append(QualityScale.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .AppendLine();

        if (TargetDescription is not null)
        {
            text.Append("target|")
                .Append(Sanitize(TargetDescription.Id))
                .Append('|')
                .Append(Sanitize(TargetDescription.Name))
                .Append('|')
                .Append(TargetDescription.Kind)
                .Append('|')
                .Append(TargetDescription.ExecutionMode)
                .Append("|features=")
                .Append(TargetDescription.Capabilities.Features)
                .Append("|stages=")
                .Append(TargetDescription.Capabilities.SupportedStages)
                .AppendLine();
        }

        foreach (var resource in Resources)
        {
            text.Append("resource|")
                .Append(resource.Id)
                .Append('|')
                .Append(resource.Kind)
                .Append('|')
                .Append(Sanitize(resource.Name))
                .Append('|')
                .Append(resource.Format)
                .Append("|size=")
                .Append(resource.Width)
                .Append('x')
                .Append(resource.Height)
                .Append("|use=")
                .Append(resource.FirstUseIndex)
                .Append("..")
                .Append(resource.LastUseIndex)
                .Append("|lifetime=")
                .Append(resource.Lifetime)
                .AppendLine();
        }

        foreach (var pass in Passes)
        {
            text.Append("pass|")
                .Append(pass.ExecutionIndex)
                .Append('|')
                .Append(pass.Id)
                .Append('|')
                .Append(Sanitize(pass.Name))
                .Append('|')
                .Append(pass.Kind)
                .Append("|stage=")
                .Append(pass.Stage)
                .Append("|depends=")
                .Append(string.Join(',', pass.Dependencies))
                .Append("|resources=")
                .Append(string.Join(',', pass.Resources))
                .Append("|features=")
                .Append(pass.RequiredFeatures)
                .AppendLine();
        }

        foreach (var barrier in Barriers)
        {
            text.Append("barrier|")
                .Append(barrier.ExecutionIndex)
                .Append('|')
                .Append(barrier.PassId)
                .Append('|')
                .Append(barrier.ResourceId)
                .Append('|')
                .Append(barrier.Before)
                .Append("->")
                .Append(barrier.After)
                .Append('|')
                .Append(barrier.Access)
                .Append('|')
                .Append(barrier.Usage)
                .AppendLine();
        }

        return text.ToString();
    }

    /// <summary>Returns deterministic compact JSON suitable for capture artifacts.</summary>
    public string ToJson()
    {
        var model = new DiagnosticJsonModel(
            Name,
            OutputSize.Width,
            OutputSize.Height,
            RenderSize.Width,
            RenderSize.Height,
            QualityProfile,
            QualityScale,
            TargetDescription?.Id,
            TargetDescription?.Name,
            TargetDescription?.Kind.ToString(),
            TargetDescription?.ExecutionMode.ToString(),
            TargetDescription?.Capabilities.Features.ToString(),
            TargetDescription?.Capabilities.SupportedStages.ToString(),
            Resources,
            Passes,
            Barriers);
        return JsonSerializer.Serialize(
            model,
            new JsonSerializerOptions
            {
                WriteIndented = false,
            });
    }

    private static string Sanitize(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');

    private sealed record DiagnosticJsonModel(
        string Name,
        int OutputWidth,
        int OutputHeight,
        int RenderWidth,
        int RenderHeight,
        string QualityProfile,
        float QualityScale,
        string? TargetId,
        string? TargetName,
        string? TargetKind,
        string? TargetExecutionMode,
        string? TargetFeatures,
        string? TargetStages,
        IReadOnlyList<FrameGraphDiagnosticResource> Resources,
        IReadOnlyList<FrameGraphDiagnosticPass> Passes,
        IReadOnlyList<FrameGraphDiagnosticBarrier> Barriers);
}
