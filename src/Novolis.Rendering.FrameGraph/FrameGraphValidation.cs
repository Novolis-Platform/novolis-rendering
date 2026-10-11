namespace Novolis.Rendering.FrameGraph;

/// <summary>Validates resource declarations, pass accesses, ordering references, and capabilities.</summary>
public static class FrameGraphValidator
{
    /// <summary>Validates a graph without allocating a compiled execution plan.</summary>
    public static FrameGraphValidationResult Validate(
        FrameGraphDescription description,
        FrameGraphTargetCapabilities? targetCapabilities = null,
        FrameGraphQualityProfile? qualityProfile = null)
    {
        ArgumentNullException.ThrowIfNull(description);

        var diagnostics = new List<FrameGraphDiagnostic>();
        var resources = description.Resources;
        var passes = description.Passes;
        var resourceMap = new Dictionary<FrameGraphResourceHandle, FrameGraphResourceDescription>();
        var initialized = new Dictionary<FrameGraphResourceHandle, bool>();

        if (description.OutputSize.Width <= 0 || description.OutputSize.Height <= 0)
        {
            Add(
                diagnostics,
                FrameGraphDiagnosticSeverity.Error,
                "FG001",
                "The graph output size must be positive.");
        }

        for (var index = 0; index < resources.Count; index++)
        {
            var resource = resources[index];
            if (resource.Handle.Id != index)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG002",
                    $"Resource '{resource.Name}' does not have a stable sequential handle.",
                    resource: resource.Handle);
            }

            if (!resourceMap.TryAdd(resource.Handle, resource))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG003",
                    $"Resource handle {resource.Handle} is declared more than once.",
                    resource: resource.Handle);
            }

            if (resource.Usage == FrameGraphResourceUsage.None)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG004",
                    $"Resource '{resource.Name}' does not declare any usage.",
                    resource: resource.Handle);
            }

            initialized[resource.Handle] = resource.Lifetime != FrameGraphResourceLifetime.Transient;
            ValidateResource(diagnostics, resource, targetCapabilities, qualityProfile);
        }

        ValidateUniqueNames(diagnostics, resources.Select(resource => resource.Name), "resource");

        for (var index = 0; index < passes.Count; index++)
        {
            var pass = passes[index];
            if (pass.Handle.Id != index)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG010",
                    $"Pass '{pass.Name}' does not have a stable sequential handle.",
                    pass: pass.Handle);
            }

            if (pass.Accesses.Count == 0)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Warning,
                    "FG011",
                    $"Pass '{pass.Name}' declares no resource accesses.",
                    pass: pass.Handle);
            }

            ValidateStage(diagnostics, pass, targetCapabilities, qualityProfile);
            ValidateFeatures(diagnostics, pass, targetCapabilities, qualityProfile);
            ValidateDependencies(diagnostics, pass, passes.Count);

            foreach (var access in pass.Accesses)
            {
                if (!resourceMap.TryGetValue(access.Resource, out var resource))
                {
                    Add(
                        diagnostics,
                        FrameGraphDiagnosticSeverity.Error,
                        "FG012",
                        $"Pass '{pass.Name}' references unknown resource {access.Resource}.",
                        pass: pass.Handle,
                        resource: access.Resource);
                    continue;
                }

                if (access.Usage == FrameGraphResourceUsage.None)
                {
                    Add(
                        diagnostics,
                        FrameGraphDiagnosticSeverity.Error,
                        "FG013",
                        $"Pass '{pass.Name}' uses resource '{resource.Name}' without a usage state.",
                        pass: pass.Handle,
                        resource: resource.Handle);
                }

                var missingUsage = access.Usage & ~resource.Usage;
                if (missingUsage != FrameGraphResourceUsage.None)
                {
                    Add(
                        diagnostics,
                        FrameGraphDiagnosticSeverity.Error,
                        "FG014",
                        $"Pass '{pass.Name}' requests {missingUsage} from resource '{resource.Name}', which does not allow it.",
                        pass: pass.Handle,
                        resource: resource.Handle);
                }

                if (access.Access is FrameGraphResourceAccessKind.Read or FrameGraphResourceAccessKind.ReadWrite
                    && !initialized[resource.Handle])
                {
                    Add(
                        diagnostics,
                        FrameGraphDiagnosticSeverity.Error,
                        "FG015",
                        $"Pass '{pass.Name}' reads transient resource '{resource.Name}' before a write.",
                        pass: pass.Handle,
                        resource: resource.Handle);
                }

                if (access.Access is FrameGraphResourceAccessKind.Write or FrameGraphResourceAccessKind.ReadWrite)
                {
                    initialized[resource.Handle] = true;
                }
            }

            ValidateAttachments(diagnostics, pass, resourceMap);
        }

        ValidateUniqueNames(diagnostics, passes.Select(pass => pass.Name), "pass");
        return new FrameGraphValidationResult(FrameGraphDescription.Freeze(diagnostics));
    }

    private static void ValidateResource(
        ICollection<FrameGraphDiagnostic> diagnostics,
        FrameGraphResourceDescription resource,
        FrameGraphTargetCapabilities? targetCapabilities,
        FrameGraphQualityProfile? qualityProfile)
    {
        if (resource is FrameGraphTextureResourceDescription texture)
        {
            var description = texture.Description;
            if (description.Format == FrameGraphFormat.Unknown)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG005",
                    $"Texture '{resource.Name}' must declare a format.",
                    resource: resource.Handle);
            }

            if (description.SizeMode == FrameGraphSizeMode.Fixed
                && (description.Size.Width <= 0 || description.Size.Height <= 0))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG006",
                    $"Texture '{resource.Name}' must have positive fixed dimensions.",
                    resource: resource.Handle);
            }

            if (description.SizeMode != FrameGraphSizeMode.Fixed
                && (!float.IsFinite(description.Scale) || description.Scale <= 0f))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG007",
                    $"Texture '{resource.Name}' must have a positive resize scale.",
                    resource: resource.Handle);
            }

            if (description.MipLevels <= 0 || description.SampleCount <= 0)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG008",
                    $"Texture '{resource.Name}' must have positive mip and sample counts.",
                    resource: resource.Handle);
            }

            if (description.Format.IsDepth()
                && description.Usage.HasFlag(FrameGraphResourceUsage.ColorAttachment))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG009",
                    $"Depth texture '{resource.Name}' cannot be used as a color attachment.",
                    resource: resource.Handle);
            }

            if (!description.Format.IsDepth()
                && description.Usage.HasFlag(FrameGraphResourceUsage.DepthStencilAttachment))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG016",
                    $"Color texture '{resource.Name}' cannot be used as a depth attachment.",
                    resource: resource.Handle);
            }

            var requiredFeature = description.Format.IsDepth()
                ? FrameGraphTargetFeature.Depth
                : description.Format.IsHdr()
                    ? FrameGraphTargetFeature.Hdr
                    : FrameGraphTargetFeature.None;
            if (requiredFeature != FrameGraphTargetFeature.None
                && qualityProfile is not null
                && !qualityProfile.IsEnabled(requiredFeature))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG030",
                    $"Texture '{resource.Name}' requires {requiredFeature}, disabled by quality profile '{qualityProfile.Name}'.",
                    resource: resource.Handle);
            }

            if (requiredFeature != FrameGraphTargetFeature.None
                && targetCapabilities is not null
                && !targetCapabilities.Supports(requiredFeature))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG031",
                    $"Texture '{resource.Name}' requires {requiredFeature}, unavailable on target '{targetCapabilities.Name}'.",
                    resource: resource.Handle);
            }
        }
        else if (resource is FrameGraphBufferResourceDescription buffer)
        {
            if (buffer.Description.SizeInBytes <= 0)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG017",
                    $"Buffer '{resource.Name}' must have a positive byte size.",
                    resource: resource.Handle);
            }

            if (buffer.Description.StrideInBytes < 0
                || (buffer.Description.StrideInBytes > 0
                    && buffer.Description.SizeInBytes % buffer.Description.StrideInBytes != 0))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG018",
                    $"Buffer '{resource.Name}' has an invalid element stride.",
                    resource: resource.Handle);
            }

            if (buffer.Description.Usage.HasFlag(FrameGraphResourceUsage.ColorAttachment)
                || buffer.Description.Usage.HasFlag(FrameGraphResourceUsage.DepthStencilAttachment))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG019",
                    $"Buffer '{resource.Name}' cannot be an attachment.",
                    resource: resource.Handle);
            }
        }
    }

    private static void ValidateStage(
        ICollection<FrameGraphDiagnostic> diagnostics,
        FrameGraphPassDescription pass,
        FrameGraphTargetCapabilities? targetCapabilities,
        FrameGraphQualityProfile? qualityProfile)
    {
        var stageMask = pass.Stage.ToMask();
        if (stageMask == FrameGraphPipelineStageMask.None)
        {
            Add(
                diagnostics,
                FrameGraphDiagnosticSeverity.Error,
                "FG032",
                $"Pass '{pass.Name}' requests an unknown pipeline stage '{pass.Stage}'.",
                pass: pass.Handle);
            return;
        }

        if (qualityProfile is not null && !qualityProfile.IsStageEnabled(pass.Stage))
        {
            Add(
                diagnostics,
                FrameGraphDiagnosticSeverity.Error,
                "FG033",
                $"Pass '{pass.Name}' requires stage '{pass.Stage}', disabled by quality profile '{qualityProfile.Name}'.",
                pass: pass.Handle);
        }

        if (targetCapabilities is not null && !targetCapabilities.SupportsStage(pass.Stage))
        {
            Add(
                diagnostics,
                FrameGraphDiagnosticSeverity.Error,
                "FG034",
                $"Pass '{pass.Name}' requires stage '{pass.Stage}', unavailable on target '{targetCapabilities.Name}'.",
                pass: pass.Handle);
        }
    }

    private static void ValidateFeatures(
        ICollection<FrameGraphDiagnostic> diagnostics,
        FrameGraphPassDescription pass,
        FrameGraphTargetCapabilities? targetCapabilities,
        FrameGraphQualityProfile? qualityProfile)
    {
        var unknown = pass.RequiredFeatures & ~FrameGraphTargetFeature.All;
        if (unknown != FrameGraphTargetFeature.None)
        {
            Add(
                diagnostics,
                FrameGraphDiagnosticSeverity.Error,
                "FG020",
                $"Pass '{pass.Name}' requests unknown target features: {unknown}.",
                pass: pass.Handle);
        }

        if (qualityProfile is not null && !qualityProfile.IsEnabled(pass.RequiredFeatures))
        {
            Add(
                diagnostics,
                FrameGraphDiagnosticSeverity.Error,
                "FG021",
                $"Pass '{pass.Name}' requires {pass.RequiredFeatures}, disabled by quality profile '{qualityProfile.Name}'.",
                pass: pass.Handle);
        }

        if (targetCapabilities is not null && !targetCapabilities.Supports(pass.RequiredFeatures))
        {
            Add(
                diagnostics,
                FrameGraphDiagnosticSeverity.Error,
                "FG022",
                $"Pass '{pass.Name}' requires {pass.RequiredFeatures}, unavailable on target '{targetCapabilities.Name}'.",
                pass: pass.Handle);
        }
    }

    private static void ValidateDependencies(
        ICollection<FrameGraphDiagnostic> diagnostics,
        FrameGraphPassDescription pass,
        int passCount)
    {
        foreach (var dependency in pass.Dependencies)
        {
            if (!dependency.IsValid || dependency.Id >= passCount)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG023",
                    $"Pass '{pass.Name}' depends on unknown pass {dependency}.",
                    pass: pass.Handle);
            }
            else if (dependency == pass.Handle)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG024",
                    $"Pass '{pass.Name}' cannot depend on itself.",
                    pass: pass.Handle);
            }
        }
    }

    private static void ValidateAttachments(
        ICollection<FrameGraphDiagnostic> diagnostics,
        FrameGraphPassDescription pass,
        IReadOnlyDictionary<FrameGraphResourceHandle, FrameGraphResourceDescription> resources)
    {
        foreach (var attachment in pass.Attachments)
        {
            var handle = (FrameGraphResourceHandle)attachment.Texture;
            if (!resources.TryGetValue(handle, out var resource))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG025",
                    $"Pass '{pass.Name}' attaches unknown texture {attachment.Texture}.",
                    pass: pass.Handle,
                    resource: handle);
                continue;
            }

            if (resource is not FrameGraphTextureResourceDescription texture)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG026",
                    $"Pass '{pass.Name}' attachment {attachment.Texture} is not a texture.",
                    pass: pass.Handle,
                    resource: handle);
                continue;
            }

            if ((resource.Usage & attachment.RequiredUsage) != attachment.RequiredUsage)
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG027",
                    $"Pass '{pass.Name}' attachment '{resource.Name}' does not declare {attachment.RequiredUsage}.",
                    pass: pass.Handle,
                    resource: handle);
            }

            var expectsDepth = attachment.Kind is FrameGraphAttachmentKind.Depth or FrameGraphAttachmentKind.Stencil;
            if (expectsDepth != texture.Description.Format.IsDepth())
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG028",
                    $"Pass '{pass.Name}' attachment '{resource.Name}' has an incompatible format.",
                    pass: pass.Handle,
                    resource: handle);
            }
        }
    }

    private static void ValidateUniqueNames(
        ICollection<FrameGraphDiagnostic> diagnostics,
        IEnumerable<string> names,
        string category)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!seen.Add(name))
            {
                Add(
                    diagnostics,
                    FrameGraphDiagnosticSeverity.Error,
                    "FG029",
                    $"Duplicate {category} name '{name}'.");
            }
        }
    }

    private static void Add(
        ICollection<FrameGraphDiagnostic> diagnostics,
        FrameGraphDiagnosticSeverity severity,
        string code,
        string message,
        FrameGraphPassHandle? pass = null,
        FrameGraphResourceHandle? resource = null) =>
        diagnostics.Add(new FrameGraphDiagnostic(severity, code, message, pass, resource));
}
