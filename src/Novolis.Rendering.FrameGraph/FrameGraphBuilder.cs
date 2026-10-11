namespace Novolis.Rendering.FrameGraph;

/// <summary>Mutable authoring builder for an immutable renderer-owned frame graph.</summary>
public sealed class FrameGraphBuilder
{
    private readonly List<FrameGraphResourceDescription> _resources = [];
    private readonly List<FrameGraphPassDescription> _passes = [];
    private string _name;
    private FrameGraphSize _outputSize;

    /// <summary>Creates a graph with a one-pixel reference output.</summary>
    public FrameGraphBuilder()
        : this("FrameGraph", new FrameGraphSize(1, 1))
    {
    }

    /// <summary>Creates a graph named <c>FrameGraph</c>.</summary>
    public FrameGraphBuilder(FrameGraphSize outputSize)
        : this("FrameGraph", outputSize)
    {
    }

    /// <summary>Creates a named graph with an initial output size.</summary>
    public FrameGraphBuilder(string name, FrameGraphSize outputSize)
    {
        ValidateName(name, nameof(name));
        _name = name;
        _outputSize = outputSize;
    }

    /// <summary>Current graph name used by diagnostics.</summary>
    public string Name => _name;

    /// <summary>Current output size used by the next build.</summary>
    public FrameGraphSize OutputSize => _outputSize;

    /// <summary>Renames the graph for subsequent descriptions.</summary>
    public FrameGraphBuilder Rename(string name)
    {
        ValidateName(name, nameof(name));
        _name = name;
        return this;
    }

    /// <summary>Changes the output size for subsequent descriptions.</summary>
    public FrameGraphBuilder Resize(FrameGraphSize outputSize)
    {
        _outputSize = outputSize;
        return this;
    }

    /// <summary>Adds a typed texture resource.</summary>
    public FrameGraphTextureHandle CreateTexture(string name, FrameGraphTextureDescription description)
    {
        ValidateResourceName(name);
        EnsureUniqueResourceName(name);
        var handle = new FrameGraphTextureHandle(_resources.Count);
        _resources.Add(new FrameGraphTextureResourceDescription(handle, name, description));
        return handle;
    }

    /// <summary>Adds an externally owned texture resource.</summary>
    public FrameGraphTextureHandle ImportTexture(string name, FrameGraphTextureDescription description)
    {
        return CreateTexture(
            name,
            description with { Lifetime = FrameGraphResourceLifetime.Imported });
    }

    /// <summary>Adds a typed buffer resource.</summary>
    public FrameGraphBufferHandle CreateBuffer(string name, FrameGraphBufferDescription description)
    {
        ValidateResourceName(name);
        EnsureUniqueResourceName(name);
        var handle = new FrameGraphBufferHandle(_resources.Count);
        _resources.Add(new FrameGraphBufferResourceDescription(handle, name, description));
        return handle;
    }

    /// <summary>Adds an externally owned buffer resource.</summary>
    public FrameGraphBufferHandle ImportBuffer(string name, FrameGraphBufferDescription description)
    {
        return CreateBuffer(
            name,
            description with { Lifetime = FrameGraphResourceLifetime.Imported });
    }

    /// <summary>Adds a configured pass in authoring order.</summary>
    public FrameGraphPassHandle AddPass(
        string name,
        FrameGraphPassKind kind,
        Action<FrameGraphPassBuilder> configure)
        => AddPass(name, kind.ToPipelineStage(), kind, configure);

    /// <summary>Adds a pass with a typed pipeline stage.</summary>
    public FrameGraphPassHandle AddPass(
        string name,
        FrameGraphPipelineStage stage,
        Action<FrameGraphPassBuilder> configure) =>
        AddPass(name, stage, stage.ToPassKind(), configure);

    /// <summary>Adds a pass with the compatibility pass-stage enum.</summary>
    public FrameGraphPassHandle AddPass(
        string name,
        FrameGraphPassStage stage,
        Action<FrameGraphPassBuilder> configure) =>
        AddPass(name, stage.ToPipelineStage(), configure);

    /// <summary>Adds a pass with an explicit stage and pass category.</summary>
    public FrameGraphPassHandle AddPass(
        string name,
        FrameGraphPipelineStage stage,
        FrameGraphPassKind kind,
        Action<FrameGraphPassBuilder> configure)
    {
        ValidateName(name, nameof(name));
        ArgumentNullException.ThrowIfNull(configure);

        var handle = new FrameGraphPassHandle(_passes.Count);
        var passBuilder = new FrameGraphPassBuilder(handle, stage);
        configure(passBuilder);
        _passes.Add(passBuilder.Build(name, kind));
        return handle;
    }

    /// <summary>Adds a compute pass using the common pass category.</summary>
    public FrameGraphPassHandle AddPass(
        string name,
        Action<FrameGraphPassBuilder> configure) =>
        AddPass(name, FrameGraphPassKind.Compute, configure);

    /// <summary>Creates the immutable graph description.</summary>
    public FrameGraphDescription Build() =>
        new(
            _name,
            _outputSize,
            FrameGraphDescription.Freeze(_resources),
            FrameGraphDescription.Freeze(_passes));

    /// <summary>Builds and compiles the current graph.</summary>
    public CompiledFrameGraph Compile(FrameGraphQualityProfile? qualityProfile = null) =>
        Build().Compile(qualityProfile);

    private void EnsureUniqueResourceName(string name)
    {
        if (_resources.Any(resource => string.Equals(resource.Name, name, StringComparison.Ordinal)))
        {
            throw new ArgumentException($"A resource named '{name}' already exists.", nameof(name));
        }
    }

    private static void ValidateResourceName(string name)
    {
        ValidateName(name, nameof(name));
        if (name.Contains('\r') || name.Contains('\n'))
        {
            throw new ArgumentException("Resource names cannot contain line breaks.", nameof(name));
        }
    }

    private static void ValidateName(string name, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A non-empty name is required.", parameterName);
        }
    }
}
