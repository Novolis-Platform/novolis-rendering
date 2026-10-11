using System.Collections.ObjectModel;

namespace Novolis.Rendering.FrameGraph;

/// <summary>Immutable renderer-owned graph description.</summary>
public sealed class FrameGraphDescription
{
    internal FrameGraphDescription(
        string name,
        FrameGraphSize outputSize,
        IReadOnlyList<FrameGraphResourceDescription> resources,
        IReadOnlyList<FrameGraphPassDescription> passes)
    {
        Name = name;
        OutputSize = outputSize;
        Resources = resources;
        Passes = passes;
    }

    /// <summary>Diagnostic graph name.</summary>
    public string Name { get; }

    /// <summary>Output size used when no compile override is supplied.</summary>
    public FrameGraphSize OutputSize { get; }

    /// <summary>Declared resources.</summary>
    public IReadOnlyList<FrameGraphResourceDescription> Resources { get; }

    /// <summary>Declared passes in authoring order.</summary>
    public IReadOnlyList<FrameGraphPassDescription> Passes { get; }

    /// <summary>Returns a description with a new output size and unchanged graph topology.</summary>
    public FrameGraphDescription WithOutputSize(FrameGraphSize outputSize) =>
        new(Name, outputSize, Resources, Passes);

    /// <summary>Validates the graph without compiling it.</summary>
    public FrameGraphValidationResult Validate(
        FrameGraphTargetCapabilities? targetCapabilities = null,
        FrameGraphQualityProfile? qualityProfile = null) =>
        FrameGraphValidator.Validate(this, targetCapabilities, qualityProfile);

    /// <summary>Validates the graph against a typed target description.</summary>
    public FrameGraphValidationResult ValidateForTarget(
        FrameGraphTargetDescription targetDescription,
        FrameGraphQualityProfile? qualityProfile = null)
    {
        ArgumentNullException.ThrowIfNull(targetDescription);
        return FrameGraphValidator.Validate(this, targetDescription.Capabilities, qualityProfile);
    }

    /// <summary>Compiles this graph with the supplied quality profile.</summary>
    public CompiledFrameGraph Compile(FrameGraphQualityProfile? qualityProfile = null) =>
        FrameGraphCompiler.Compile(
            this,
            new FrameGraphCompileOptions { QualityProfile = qualityProfile ?? FrameGraphQualityProfile.Default });

    /// <summary>Compiles this graph against a typed target description.</summary>
    public CompiledFrameGraph CompileForTarget(
        FrameGraphTargetDescription targetDescription,
        FrameGraphQualityProfile? qualityProfile = null)
    {
        ArgumentNullException.ThrowIfNull(targetDescription);
        return FrameGraphCompiler.Compile(
            this,
            new FrameGraphCompileOptions
            {
                QualityProfile = qualityProfile ?? FrameGraphQualityProfile.Default,
                TargetDescription = targetDescription,
            });
    }

    internal static IReadOnlyList<T> Freeze<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}
