namespace Novolis.Rendering.FrameGraph;

/// <summary>Compiles renderer-owned graph descriptions into deterministic plans.</summary>
public static class FrameGraphCompiler
{
    /// <summary>Compiles a graph or throws a validation exception.</summary>
    public static CompiledFrameGraph Compile(
        FrameGraphDescription description,
        FrameGraphCompileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(description);
        options ??= new FrameGraphCompileOptions();

        var qualityProfile = options.QualityProfile ?? FrameGraphQualityProfile.Default;
        var effectiveDescription = options.OutputSize is { } requestedOutputSize
            ? description.WithOutputSize(requestedOutputSize)
            : description;
        var validation = FrameGraphValidator.Validate(
            effectiveDescription,
            options.TargetDescription?.Capabilities ?? options.TargetCapabilities,
            qualityProfile);
        validation.ThrowIfInvalid();

        var dependencies = BuildDependencies(effectiveDescription);
        var order = TopologicalOrder(effectiveDescription.Passes.Count, dependencies);
        if (order.Count != effectiveDescription.Passes.Count)
        {
            var diagnostics = validation.Diagnostics.ToList();
            diagnostics.Add(
                new FrameGraphDiagnostic(
                    FrameGraphDiagnosticSeverity.Error,
                    "FG200",
                    "Pass dependencies contain a cycle."));
            throw new FrameGraphValidationException(FrameGraphDescription.Freeze(diagnostics));
        }

        var outputSize = effectiveDescription.OutputSize;
        var renderSize = outputSize.Scale(qualityProfile.RenderScale);
        var resources = CompileResources(effectiveDescription, outputSize, qualityProfile.RenderScale, order);
        var barriersByPass = BuildBarriers(
            effectiveDescription,
            order,
            options.IncludeInitialTransitions);
        var compiledPasses = CompilePasses(
            effectiveDescription,
            order,
            dependencies,
            barriersByPass);
        var dependencyList = dependencies
            .Values
            .Select(value => value.ToDependency())
            .OrderBy(dependency => dependency.To.Id)
            .ThenBy(dependency => dependency.From.Id)
            .ToArray();
        var barriers = compiledPasses
            .SelectMany(pass => pass.Barriers)
            .ToArray();

        return new CompiledFrameGraph(
            effectiveDescription,
            outputSize,
            renderSize,
            qualityProfile,
            options.TargetDescription?.Capabilities ?? options.TargetCapabilities,
            options.TargetDescription,
            resources,
            compiledPasses,
            FrameGraphDescription.Freeze(dependencyList),
            FrameGraphDescription.Freeze(barriers),
            options.IncludeInitialTransitions);
    }

    /// <summary>Attempts compilation and returns diagnostics instead of throwing.</summary>
    public static FrameGraphCompilationResult TryCompile(
        FrameGraphDescription description,
        FrameGraphCompileOptions? options = null)
    {
        try
        {
            var plan = Compile(description, options);
            return new FrameGraphCompilationResult(plan, Array.Empty<FrameGraphDiagnostic>());
        }
        catch (FrameGraphValidationException exception)
        {
            return new FrameGraphCompilationResult(null, exception.Diagnostics);
        }
    }

    private static IReadOnlyList<FrameGraphCompiledResource> CompileResources(
        FrameGraphDescription description,
        FrameGraphSize outputSize,
        float qualityScale,
        IReadOnlyList<int> order)
    {
        var firstUse = description.Resources.ToDictionary(resource => resource.Handle, _ => -1);
        var lastUse = description.Resources.ToDictionary(resource => resource.Handle, _ => -1);
        for (var executionIndex = 0; executionIndex < order.Count; executionIndex++)
        {
            foreach (var access in description.Passes[order[executionIndex]].Accesses)
            {
                if (firstUse[access.Resource] < 0)
                {
                    firstUse[access.Resource] = executionIndex;
                }

                lastUse[access.Resource] = executionIndex;
            }
        }

        return FrameGraphDescription.Freeze(
            description.Resources.Select(
                resource => new FrameGraphCompiledResource(
                    resource,
                    resource.ResolveSize(outputSize, qualityScale),
                    firstUse[resource.Handle],
                    lastUse[resource.Handle])));
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<FrameGraphBarrier>> BuildBarriers(
        FrameGraphDescription description,
        IReadOnlyList<int> order,
        bool includeInitialTransitions)
    {
        var states = description.Resources.ToDictionary(
            resource => resource.Handle,
            resource => resource.Lifetime == FrameGraphResourceLifetime.Imported
                ? FrameGraphResourceState.External
                : FrameGraphResourceState.Undefined);
        var barriers = new Dictionary<int, IReadOnlyList<FrameGraphBarrier>>();

        for (var executionIndex = 0; executionIndex < order.Count; executionIndex++)
        {
            var pass = description.Passes[order[executionIndex]];
            var passBarriers = new List<FrameGraphBarrier>();
            foreach (var access in pass.Accesses)
            {
                var before = states[access.Resource];
                var after = MapState(access);
                if (before != after
                    && (includeInitialTransitions
                        || (before is not FrameGraphResourceState.Undefined
                            and not FrameGraphResourceState.External)))
                {
                    passBarriers.Add(
                        new FrameGraphBarrier(
                            executionIndex,
                            pass.Handle,
                            access.Resource,
                            before,
                            after,
                            access.Access,
                            access.Usage));
                }

                states[access.Resource] = after;
            }

            barriers[pass.Handle.Id] = FrameGraphDescription.Freeze(passBarriers);
        }

        return barriers;
    }

    private static IReadOnlyList<FrameGraphCompiledPass> CompilePasses(
        FrameGraphDescription description,
        IReadOnlyList<int> order,
        IReadOnlyDictionary<(int From, int To), DependencyAccumulator> dependencies,
        IReadOnlyDictionary<int, IReadOnlyList<FrameGraphBarrier>> barriersByPass)
    {
        var result = new List<FrameGraphCompiledPass>(order.Count);
        for (var executionIndex = 0; executionIndex < order.Count; executionIndex++)
        {
            var passIndex = order[executionIndex];
            var pass = description.Passes[passIndex];
            var dependenciesForPass = dependencies
                .Where(edge => edge.Key.To == passIndex)
                .Select(edge => new FrameGraphPassHandle(edge.Key.From))
                .OrderBy(handle => handle.Id)
                .ToArray();
            result.Add(
                new FrameGraphCompiledPass(
                    pass,
                    executionIndex,
                    FrameGraphDescription.Freeze(dependenciesForPass),
                    barriersByPass[pass.Handle.Id]));
        }

        return FrameGraphDescription.Freeze(result);
    }

    private static Dictionary<(int From, int To), DependencyAccumulator> BuildDependencies(
        FrameGraphDescription description)
    {
        var dependencies = new Dictionary<(int From, int To), DependencyAccumulator>();
        foreach (var pass in description.Passes)
        {
            foreach (var dependency in pass.Dependencies)
            {
                AddDependency(
                    dependencies,
                    dependency.Id,
                    pass.Handle.Id,
                    FrameGraphDependencyReason.Explicit,
                    null);
            }
        }

        var lastWriter = new Dictionary<FrameGraphResourceHandle, int>();
        var readers = description.Resources.ToDictionary(
            resource => resource.Handle,
            _ => new HashSet<int>());
        foreach (var pass in description.Passes)
        {
            foreach (var access in pass.Accesses)
            {
                var passIndex = pass.Handle.Id;
                var reads = access.Access is FrameGraphResourceAccessKind.Read
                    or FrameGraphResourceAccessKind.ReadWrite;
                var writes = access.Access is FrameGraphResourceAccessKind.Write
                    or FrameGraphResourceAccessKind.ReadWrite;

                if (reads && lastWriter.TryGetValue(access.Resource, out var writer))
                {
                    AddDependency(
                        dependencies,
                        writer,
                        passIndex,
                        FrameGraphDependencyReason.ResourceReadAfterWrite,
                        access.Resource);
                }

                if (reads)
                {
                    readers[access.Resource].Add(passIndex);
                }

                if (writes)
                {
                    if (lastWriter.TryGetValue(access.Resource, out writer))
                    {
                        AddDependency(
                            dependencies,
                            writer,
                            passIndex,
                            FrameGraphDependencyReason.ResourceWriteAfterWrite,
                            access.Resource);
                    }

                    foreach (var reader in readers[access.Resource])
                    {
                        AddDependency(
                            dependencies,
                            reader,
                            passIndex,
                            FrameGraphDependencyReason.ResourceWriteAfterRead,
                            access.Resource);
                    }

                    readers[access.Resource].Clear();
                    lastWriter[access.Resource] = passIndex;
                }
            }
        }

        return dependencies;
    }

    private static IReadOnlyList<int> TopologicalOrder(
        int passCount,
        IReadOnlyDictionary<(int From, int To), DependencyAccumulator> dependencies)
    {
        var outgoing = Enumerable.Range(0, passCount)
            .Select(_ => new List<int>())
            .ToArray();
        var indegree = new int[passCount];
        foreach (var edge in dependencies.Keys)
        {
            outgoing[edge.From].Add(edge.To);
            indegree[edge.To]++;
        }

        var ready = new PriorityQueue<int, int>();
        for (var index = 0; index < passCount; index++)
        {
            if (indegree[index] == 0)
            {
                ready.Enqueue(index, index);
            }
        }

        var order = new List<int>(passCount);
        while (ready.TryDequeue(out var next, out _))
        {
            order.Add(next);
            foreach (var dependent in outgoing[next].OrderBy(index => index))
            {
                indegree[dependent]--;
                if (indegree[dependent] == 0)
                {
                    ready.Enqueue(dependent, dependent);
                }
            }
        }

        return order;
    }

    private static void AddDependency(
        IDictionary<(int From, int To), DependencyAccumulator> dependencies,
        int from,
        int to,
        FrameGraphDependencyReason reason,
        FrameGraphResourceHandle? resource)
    {
        if (from == to)
        {
            return;
        }

        var key = (from, to);
        if (!dependencies.TryGetValue(key, out var accumulator))
        {
            accumulator = new DependencyAccumulator(from, to);
            dependencies.Add(key, accumulator);
        }

        accumulator.Reason |= reason;
        if (accumulator.Resource is null)
        {
            accumulator.Resource = resource;
        }
        else if (resource is not null && accumulator.Resource != resource)
        {
            accumulator.Resource = null;
        }
    }

    private static FrameGraphResourceState MapState(FrameGraphResourceAccess access)
    {
        if (access.Usage.HasFlag(FrameGraphResourceUsage.Present))
        {
            return FrameGraphResourceState.Present;
        }

        if (access.Usage.HasFlag(FrameGraphResourceUsage.ColorAttachment))
        {
            return FrameGraphResourceState.ColorAttachment;
        }

        if (access.Usage.HasFlag(FrameGraphResourceUsage.DepthStencilAttachment))
        {
            return access.Access == FrameGraphResourceAccessKind.Read
                ? FrameGraphResourceState.DepthRead
                : FrameGraphResourceState.DepthWrite;
        }

        if (access.Usage.HasFlag(FrameGraphResourceUsage.TransferDestination)
            && access.Access != FrameGraphResourceAccessKind.Read)
        {
            return FrameGraphResourceState.TransferWrite;
        }

        if (access.Usage.HasFlag(FrameGraphResourceUsage.TransferSource))
        {
            return FrameGraphResourceState.TransferRead;
        }

        if (access.Usage.HasFlag(FrameGraphResourceUsage.StorageWrite)
            && access.Access != FrameGraphResourceAccessKind.Read)
        {
            return FrameGraphResourceState.ShaderWrite;
        }

        if (access.Usage.HasFlag(FrameGraphResourceUsage.Vertex))
        {
            return FrameGraphResourceState.VertexInput;
        }

        if (access.Usage.HasFlag(FrameGraphResourceUsage.Index))
        {
            return FrameGraphResourceState.IndexInput;
        }

        if (access.Usage.HasFlag(FrameGraphResourceUsage.Uniform))
        {
            return FrameGraphResourceState.UniformRead;
        }

        return FrameGraphResourceState.ShaderRead;
    }

    private sealed class DependencyAccumulator(int from, int to)
    {
        public int From { get; } = from;
        public int To { get; } = to;
        public FrameGraphDependencyReason Reason { get; set; }
        public FrameGraphResourceHandle? Resource { get; set; }

        public FrameGraphDependency ToDependency() =>
            new(new FrameGraphPassHandle(From), new FrameGraphPassHandle(To), Reason, Resource);
    }
}
