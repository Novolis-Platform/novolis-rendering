using System.Numerics;
using System.Runtime.InteropServices;
using Novolis.Math.Geometry;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>
/// Interactive CAD wireframe renderer (graphics pipeline, line list) with CPU RGBA readback.
/// Separate from <see cref="VulkanRayTracingBackend"/> compute path tracing.
/// </summary>
public sealed unsafe class VulkanWireframeRenderer : IDisposable
{
    private readonly Vk _vk = Vk.GetApi();
    private Instance _instance;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _graphicsQueue;
    private uint _graphicsQueueFamily;
    private CommandPool _commandPool;
    private CommandBuffer _commandBuffer;
    private Fence _fence;
    private RenderPass _renderPass;
    private PipelineLayout _pipelineLayout;
    private Pipeline _pipeline;
    private ShaderModule _vertModule;
    private ShaderModule _fragModule;
    private Image _colorImage;
    private DeviceMemory _colorMemory;
    private ImageView _colorView;
    private Framebuffer _framebuffer;
    private Buffer _stagingBuffer;
    private DeviceMemory _stagingMemory;
    private void* _stagingMapped;
    private Buffer _vertexBuffer;
    private DeviceMemory _vertexMemory;
    private void* _vertexMapped;
    private ulong _vertexCapacity;
    private int _width;
    private int _height;
    private bool _initialized;
    private bool _disposed;

    /// <summary>Human-readable GPU name.</summary>
    public string DeviceName { get; private set; } = "Vulkan";

    /// <summary>Last render width in pixels.</summary>
    public int Width => _width;

    /// <summary>Last render height in pixels.</summary>
    public int Height => _height;

    /// <summary>Tries to create and initialize a wireframe renderer. Returns false when Vulkan is unavailable.</summary>
    public static bool TryCreate(out VulkanWireframeRenderer? renderer)
    {
        try
        {
            var created = new VulkanWireframeRenderer();
            created.Initialize();
            renderer = created;
            return true;
        }
        catch
        {
            renderer = null;
            return false;
        }
    }

    /// <summary>Creates and initializes the renderer (throws if Vulkan is unavailable).</summary>
    public static VulkanWireframeRenderer Create()
    {
        var renderer = new VulkanWireframeRenderer();
        renderer.Initialize();
        return renderer;
    }

    private VulkanWireframeRenderer()
    {
    }

    /// <summary>Ensures an offscreen color target of the given size.</summary>
    public void Resize(int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();
        width = System.Math.Max(1, width);
        height = System.Math.Max(1, height);
        if (width == _width && height == _height && _colorImage.Handle != 0)
            return;

        _vk.DeviceWaitIdle(_device);
        DestroyFrameResources();
        _width = width;
        _height = height;
        CreateColorTarget();
        CreateStagingBuffer();
        CreateFramebuffer();
    }

    /// <summary>
    /// Clears the target, draws <paramref name="vertices"/> as a line list with <paramref name="mvp"/>,
    /// then copies pixels into host memory for <see cref="Readback"/>.
    /// </summary>
    public void Render(ReadOnlySpan<VulkanWireVertex> vertices, Matrix4x4 mvp, Rgba32 clearColor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();
        if (_width <= 0 || _height <= 0)
            throw new InvalidOperationException("Call Resize before Render.");

        EnsureVertexCapacity((ulong)System.Math.Max(2, vertices.Length));
        if (vertices.Length > 0)
        {
            fixed (VulkanWireVertex* src = vertices)
                System.Buffer.MemoryCopy(src, _vertexMapped, (nuint)(vertices.Length * sizeof(VulkanWireVertex)), (nuint)(vertices.Length * sizeof(VulkanWireVertex)));
        }

        // Column-major for GLSL mat4 with transpose=false when reading Numerics memory as columns.
        var mvpGl = mvp;

        _vk.WaitForFences(_device, 1, in _fence, true, ulong.MaxValue);
        _vk.ResetFences(_device, 1, in _fence);
        _vk.ResetCommandBuffer(_commandBuffer, 0);

        var begin = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };
        Check(_vk.BeginCommandBuffer(_commandBuffer, in begin), "BeginCommandBuffer");

        TransitionImage(_colorImage, ImageLayout.Undefined, ImageLayout.ColorAttachmentOptimal,
            AccessFlags.None, AccessFlags.ColorAttachmentWriteBit,
            PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.ColorAttachmentOutputBit);

        var clear = new ClearValue
        {
            Color = new ClearColorValue(clearColor.R / 255f, clearColor.G / 255f, clearColor.B / 255f, clearColor.A / 255f),
        };
        var renderArea = new Rect2D { Extent = new Extent2D((uint)_width, (uint)_height) };
        var rpBegin = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _renderPass,
            Framebuffer = _framebuffer,
            RenderArea = renderArea,
            ClearValueCount = 1,
            PClearValues = &clear,
        };
        _vk.CmdBeginRenderPass(_commandBuffer, in rpBegin, SubpassContents.Inline);
        _vk.CmdBindPipeline(_commandBuffer, PipelineBindPoint.Graphics, _pipeline);

        var viewport = new Viewport
        {
            Width = _width,
            Height = _height,
            MinDepth = 0f,
            MaxDepth = 1f,
        };
        _vk.CmdSetViewport(_commandBuffer, 0, 1, &viewport);
        var scissor = new Rect2D { Extent = new Extent2D((uint)_width, (uint)_height) };
        _vk.CmdSetScissor(_commandBuffer, 0, 1, &scissor);

        _vk.CmdPushConstants(_commandBuffer, _pipelineLayout, ShaderStageFlags.VertexBit, 0, 64, &mvpGl);

        if (vertices.Length >= 2)
        {
            var vb = _vertexBuffer;
            ulong offset = 0;
            _vk.CmdBindVertexBuffers(_commandBuffer, 0, 1, in vb, in offset);
            _vk.CmdDraw(_commandBuffer, (uint)vertices.Length, 1, 0, 0);
        }

        _vk.CmdEndRenderPass(_commandBuffer);

        TransitionImage(_colorImage, ImageLayout.ColorAttachmentOptimal, ImageLayout.TransferSrcOptimal,
            AccessFlags.ColorAttachmentWriteBit, AccessFlags.TransferReadBit,
            PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TransferBit);

        var region = new BufferImageCopy
        {
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LayerCount = 1,
            },
            ImageExtent = new Extent3D((uint)_width, (uint)_height, 1),
        };
        _vk.CmdCopyImageToBuffer(_commandBuffer, _colorImage, ImageLayout.TransferSrcOptimal, _stagingBuffer, 1, in region);
        _vk.EndCommandBuffer(_commandBuffer);

        var submit = new SubmitInfo { SType = StructureType.SubmitInfo, CommandBufferCount = 1 };
        fixed (CommandBuffer* cmd = &_commandBuffer)
        {
            submit.PCommandBuffers = cmd;
            Check(_vk.QueueSubmit(_graphicsQueue, 1, in submit, _fence), "QueueSubmit");
        }

        _vk.WaitForFences(_device, 1, in _fence, true, ulong.MaxValue);
    }

    /// <summary>Copies the last rendered frame into <paramref name="pixels"/> (length must be Width*Height).</summary>
    public void Readback(Span<Rgba32> pixels)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_stagingMapped == null || pixels.Length != _width * _height)
            throw new ArgumentException("Pixel buffer must match Width*Height.", nameof(pixels));

        var src = (byte*)_stagingMapped;
        for (var i = 0; i < pixels.Length; i++)
        {
            var o = i * 4;
            pixels[i] = new Rgba32(src[o], src[o + 1], src[o + 2], src[o + 3]);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_device.Handle != 0)
        {
            _vk.DeviceWaitIdle(_device);
            DestroyFrameResources();
            DestroyVertexBuffer();
            if (_pipeline.Handle != 0) _vk.DestroyPipeline(_device, _pipeline, null);
            if (_pipelineLayout.Handle != 0) _vk.DestroyPipelineLayout(_device, _pipelineLayout, null);
            if (_vertModule.Handle != 0) _vk.DestroyShaderModule(_device, _vertModule, null);
            if (_fragModule.Handle != 0) _vk.DestroyShaderModule(_device, _fragModule, null);
            if (_renderPass.Handle != 0) _vk.DestroyRenderPass(_device, _renderPass, null);
            if (_fence.Handle != 0) _vk.DestroyFence(_device, _fence, null);
            if (_commandPool.Handle != 0) _vk.DestroyCommandPool(_device, _commandPool, null);
            _vk.DestroyDevice(_device, null);
        }

        if (_instance.Handle != 0)
            _vk.DestroyInstance(_instance, null);
        _vk.Dispose();
    }

    private void Initialize()
    {
        if (_initialized)
            return;
        CreateInstance();
        _physicalDevice = PickPhysicalDevice();
        _vk.GetPhysicalDeviceProperties(_physicalDevice, out var props);
        DeviceName = $"Vulkan ({Marshal.PtrToStringAnsi((nint)props.DeviceName) ?? "GPU"})";
        CreateDevice();
        CreateCommandResources();
        CreateRenderPass();
        CreatePipeline();
        _initialized = true;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            Initialize();
    }

    private void CreateInstance()
    {
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            ApiVersion = Vk.Version12,
        };
        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
        };
        Check(_vk.CreateInstance(in createInfo, null, out _instance), "CreateInstance");
    }

    private PhysicalDevice PickPhysicalDevice()
    {
        uint count = 0;
        _vk.EnumeratePhysicalDevices(_instance, ref count, null);
        if (count == 0)
            throw new InvalidOperationException("No Vulkan physical devices.");

        var devices = new PhysicalDevice[count];
        fixed (PhysicalDevice* p = devices)
            _vk.EnumeratePhysicalDevices(_instance, ref count, p);

        PhysicalDevice? discrete = null;
        PhysicalDevice? any = null;
        foreach (var device in devices)
        {
            if (!FindGraphicsQueueFamily(device, out _))
                continue;
            any ??= device;
            _vk.GetPhysicalDeviceProperties(device, out var props);
            if (props.DeviceType == PhysicalDeviceType.DiscreteGpu)
                discrete = device;
        }

        return discrete ?? any ?? throw new InvalidOperationException("No Vulkan device with graphics queue.");
    }

    private bool FindGraphicsQueueFamily(PhysicalDevice device, out uint index)
    {
        uint count = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, null);
        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* p = families)
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, p);

        for (uint i = 0; i < families.Length; i++)
        {
            if ((families[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
            {
                index = i;
                return true;
            }
        }

        index = 0;
        return false;
    }

    private void CreateDevice()
    {
        FindGraphicsQueueFamily(_physicalDevice, out _graphicsQueueFamily);
        var priority = 1f;
        var queueCreate = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = _graphicsQueueFamily,
            QueueCount = 1,
            PQueuePriorities = &priority,
        };
        var deviceCreate = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreate,
        };
        Check(_vk.CreateDevice(_physicalDevice, in deviceCreate, null, out _device), "CreateDevice");
        _vk.GetDeviceQueue(_device, _graphicsQueueFamily, 0, out _graphicsQueue);
    }

    private void CreateCommandResources()
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _graphicsQueueFamily,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
        };
        Check(_vk.CreateCommandPool(_device, in poolInfo, null, out _commandPool), "CreateCommandPool");

        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        Check(_vk.AllocateCommandBuffers(_device, in allocInfo, out _commandBuffer), "AllocateCommandBuffers");

        var fenceInfo = new FenceCreateInfo { SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.SignaledBit };
        Check(_vk.CreateFence(_device, in fenceInfo, null, out _fence), "CreateFence");
    }

    private void CreateRenderPass()
    {
        var colorAttachment = new AttachmentDescription
        {
            Format = Format.R8G8B8A8Unorm,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.ColorAttachmentOptimal,
        };
        var colorRef = new AttachmentReference { Attachment = 0, Layout = ImageLayout.ColorAttachmentOptimal };
        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorRef,
        };
        var dependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit,
        };
        var rpInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency,
        };
        Check(_vk.CreateRenderPass(_device, in rpInfo, null, out _renderPass), "CreateRenderPass");
    }

    private void CreatePipeline()
    {
        _vertModule = CreateShaderModule(VulkanShaderCompiler.GetWireVertexSpirv());
        _fragModule = CreateShaderModule(VulkanShaderCompiler.GetWireFragmentSpirv());

        var vertName = (byte*)SilkMarshal.StringToPtr("main");
        var fragName = (byte*)SilkMarshal.StringToPtr("main");
        try
        {
            var stages = stackalloc PipelineShaderStageCreateInfo[2];
            stages[0] = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = _vertModule,
                PName = vertName,
            };
            stages[1] = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = _fragModule,
                PName = fragName,
            };

            var binding = new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = (uint)sizeof(VulkanWireVertex),
                InputRate = VertexInputRate.Vertex,
            };
            var attrs = stackalloc VertexInputAttributeDescription[2];
            attrs[0] = new VertexInputAttributeDescription
            {
                Location = 0,
                Binding = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = 0,
            };
            attrs[1] = new VertexInputAttributeDescription
            {
                Location = 1,
                Binding = 0,
                Format = Format.R32G32B32A32Sfloat,
                Offset = (uint)sizeof(Vector3),
            };
            var vertexInput = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                PVertexBindingDescriptions = &binding,
                VertexAttributeDescriptionCount = 2,
                PVertexAttributeDescriptions = attrs,
            };
            var inputAssembly = new PipelineInputAssemblyStateCreateInfo
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.LineList,
            };
            var viewportState = new PipelineViewportStateCreateInfo
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                ScissorCount = 1,
            };
            var raster = new PipelineRasterizationStateCreateInfo
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModeFlags.None,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1f,
            };
            var multisample = new PipelineMultisampleStateCreateInfo
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.Count1Bit,
            };
            var blendAttachment = new PipelineColorBlendAttachmentState
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            };
            var blend = new PipelineColorBlendStateCreateInfo
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                AttachmentCount = 1,
                PAttachments = &blendAttachment,
            };
            var dynamicStates = stackalloc DynamicState[] { DynamicState.Viewport, DynamicState.Scissor };
            var dynamic = new PipelineDynamicStateCreateInfo
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                PDynamicStates = dynamicStates,
            };

            var pushRange = new PushConstantRange
            {
                StageFlags = ShaderStageFlags.VertexBit,
                Offset = 0,
                Size = 64,
            };
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 1,
                PPushConstantRanges = &pushRange,
            };
            Check(_vk.CreatePipelineLayout(_device, in layoutInfo, null, out _pipelineLayout), "CreatePipelineLayout");

            var pipelineInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = stages,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &raster,
                PMultisampleState = &multisample,
                PColorBlendState = &blend,
                PDynamicState = &dynamic,
                Layout = _pipelineLayout,
                RenderPass = _renderPass,
                Subpass = 0,
            };
            Check(_vk.CreateGraphicsPipelines(_device, default, 1, in pipelineInfo, null, out _pipeline), "CreateGraphicsPipelines");
        }
        finally
        {
            SilkMarshal.Free((nint)vertName);
            SilkMarshal.Free((nint)fragName);
        }
    }

    private ShaderModule CreateShaderModule(ReadOnlySpan<byte> spirv)
    {
        fixed (byte* code = spirv)
        {
            var info = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirv.Length,
                PCode = (uint*)code,
            };
            Check(_vk.CreateShaderModule(_device, in info, null, out var module), "CreateShaderModule");
            return module;
        }
    }

    private void CreateColorTarget()
    {
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = Format.R8G8B8A8Unorm,
            Extent = new Extent3D((uint)_width, (uint)_height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit,
            InitialLayout = ImageLayout.Undefined,
        };
        Check(_vk.CreateImage(_device, in imageInfo, null, out _colorImage), "CreateImage");
        _vk.GetImageMemoryRequirements(_device, _colorImage, out var req);
        var alloc = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = req.Size,
            MemoryTypeIndex = FindMemoryType(req.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit),
        };
        Check(_vk.AllocateMemory(_device, in alloc, null, out _colorMemory), "AllocateMemory(color)");
        Check(_vk.BindImageMemory(_device, _colorImage, _colorMemory, 0), "BindImageMemory");

        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _colorImage,
            ViewType = ImageViewType.Type2D,
            Format = Format.R8G8B8A8Unorm,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LevelCount = 1,
                LayerCount = 1,
            },
        };
        Check(_vk.CreateImageView(_device, in viewInfo, null, out _colorView), "CreateImageView");
    }

    private void CreateStagingBuffer()
    {
        var size = (ulong)_width * (ulong)_height * 4UL;
        CreateHostBuffer(size, BufferUsageFlags.TransferDstBit, out _stagingBuffer, out _stagingMemory, out _stagingMapped);
    }

    private void CreateFramebuffer()
    {
        var attachment = _colorView;
        var fbInfo = new FramebufferCreateInfo
        {
            SType = StructureType.FramebufferCreateInfo,
            RenderPass = _renderPass,
            AttachmentCount = 1,
            PAttachments = &attachment,
            Width = (uint)_width,
            Height = (uint)_height,
            Layers = 1,
        };
        Check(_vk.CreateFramebuffer(_device, in fbInfo, null, out _framebuffer), "CreateFramebuffer");
    }

    private void EnsureVertexCapacity(ulong vertexCount)
    {
        var bytes = vertexCount * (ulong)sizeof(VulkanWireVertex);
        if (_vertexMapped != null && _vertexCapacity >= bytes)
            return;

        DestroyVertexBuffer();
        _vertexCapacity = System.Math.Max(bytes, 64UL * (ulong)sizeof(VulkanWireVertex));
        CreateHostBuffer(_vertexCapacity, BufferUsageFlags.VertexBufferBit, out _vertexBuffer, out _vertexMemory, out _vertexMapped);
    }

    private void CreateHostBuffer(ulong size, BufferUsageFlags usage, out Buffer buffer, out DeviceMemory memory, out void* mapped)
    {
        mapped = null;
        var info = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };
        Check(_vk.CreateBuffer(_device, in info, null, out buffer), "CreateBuffer");
        _vk.GetBufferMemoryRequirements(_device, buffer, out var req);
        var alloc = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = req.Size,
            MemoryTypeIndex = FindMemoryType(
                req.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit),
        };
        Check(_vk.AllocateMemory(_device, in alloc, null, out memory), "AllocateMemory(host)");
        Check(_vk.BindBufferMemory(_device, buffer, memory, 0), "BindBufferMemory");
        void* ptr = null;
        Check(_vk.MapMemory(_device, memory, 0, req.Size, 0, ref ptr), "MapMemory");
        mapped = ptr;
    }

    private uint FindMemoryType(uint typeBits, MemoryPropertyFlags flags)
    {
        _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out var props);
        for (uint i = 0; i < props.MemoryTypeCount; i++)
        {
            if ((typeBits & (1u << (int)i)) == 0)
                continue;
            if ((props.MemoryTypes[(int)i].PropertyFlags & flags) == flags)
                return i;
        }

        throw new InvalidOperationException($"No Vulkan memory type for {flags}.");
    }

    private void TransitionImage(
        Image image,
        ImageLayout oldLayout,
        ImageLayout newLayout,
        AccessFlags srcAccess,
        AccessFlags dstAccess,
        PipelineStageFlags srcStage,
        PipelineStageFlags dstStage)
    {
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcAccessMask = srcAccess,
            DstAccessMask = dstAccess,
            Image = image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LevelCount = 1,
                LayerCount = 1,
            },
        };
        _vk.CmdPipelineBarrier(_commandBuffer, srcStage, dstStage, 0, 0, null, 0, null, 1, in barrier);
    }

    private void DestroyFrameResources()
    {
        if (_framebuffer.Handle != 0) { _vk.DestroyFramebuffer(_device, _framebuffer, null); _framebuffer = default; }
        if (_colorView.Handle != 0) { _vk.DestroyImageView(_device, _colorView, null); _colorView = default; }
        if (_colorImage.Handle != 0) { _vk.DestroyImage(_device, _colorImage, null); _colorImage = default; }
        if (_colorMemory.Handle != 0) { _vk.FreeMemory(_device, _colorMemory, null); _colorMemory = default; }
        if (_stagingMapped != null) { _vk.UnmapMemory(_device, _stagingMemory); _stagingMapped = null; }
        if (_stagingBuffer.Handle != 0) { _vk.DestroyBuffer(_device, _stagingBuffer, null); _stagingBuffer = default; }
        if (_stagingMemory.Handle != 0) { _vk.FreeMemory(_device, _stagingMemory, null); _stagingMemory = default; }
    }

    private void DestroyVertexBuffer()
    {
        if (_vertexMapped != null) { _vk.UnmapMemory(_device, _vertexMemory); _vertexMapped = null; }
        if (_vertexBuffer.Handle != 0) { _vk.DestroyBuffer(_device, _vertexBuffer, null); _vertexBuffer = default; }
        if (_vertexMemory.Handle != 0) { _vk.FreeMemory(_device, _vertexMemory, null); _vertexMemory = default; }
        _vertexCapacity = 0;
    }

    private static void Check(Result result, string what)
    {
        if (result != Result.Success)
            throw new InvalidOperationException($"Vulkan {what} failed: {result}");
    }
}
