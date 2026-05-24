using System.Runtime.InteropServices;
using Novolis.Math.Geometry;
using Novolis.Rendering.Runtime;
using Silk.NET.Vulkan;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>Vulkan compute device for path tracing (SPIR-V compute pipeline).</summary>
internal sealed unsafe class VulkanComputeDevice : IDisposable
{
    private readonly Vk _vk = Vk.GetApi();
    private Instance _instance;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _computeQueue;
    private uint _computeQueueFamily;
    private CommandPool _commandPool;
    private CommandBuffer _commandBuffer;
    private Fence _fence;
    private DescriptorPool _descriptorPool;
    private DescriptorSetLayout _descriptorSetLayout;
    private DescriptorSet _descriptorSet;
    private PipelineLayout _pipelineLayout;
    private Pipeline _pipeline;
    private ShaderModule _shaderModule;
    private DeviceMemory _uniformMemory;
    private Silk.NET.Vulkan.Buffer _uniformBuffer;
    private void* _uniformMapped;
    private DeviceMemory _accumulationMemory;
    private Silk.NET.Vulkan.Buffer _accumulationBuffer;
    private DeviceMemory _displayMemory;
    private Silk.NET.Vulkan.Buffer _displayBuffer;
    private void* _displayMapped;
    private DeviceMemory _trianglesMemory;
    private Silk.NET.Vulkan.Buffer _trianglesBuffer;
    private DeviceMemory _materialsMemory;
    private Silk.NET.Vulkan.Buffer _materialsBuffer;
    private DeviceMemory _lightsMemory;
    private Silk.NET.Vulkan.Buffer _lightsBuffer;
    private DeviceMemory _bvhMemory;
    private Silk.NET.Vulkan.Buffer _bvhBuffer;
    private DeviceMemory _orderMemory;
    private Silk.NET.Vulkan.Buffer _orderBuffer;
    private int _width;
    private int _height;
    private ulong _pixelCount;
    private bool _disposed;

    public string DeviceName { get; private set; } = "Vulkan";

    public nint DisplayBufferHandle => (nint)_displayBuffer.Handle;

    public void Initialize()
    {
        if (_device.Handle != 0)
        {
            return;
        }

        CreateInstance();
        _physicalDevice = PickPhysicalDevice();
        _vk.GetPhysicalDeviceProperties(_physicalDevice, out var props);
        DeviceName = $"Vulkan ({Marshal.PtrToStringAnsi((nint)props.DeviceName) ?? "GPU"})";
        CreateDevice();
        CreateCommandResources();
        CreatePipeline();
        CreateDescriptorPoolAndSet();
    }

    public void Resize(int width, int height)
    {
        EnsureDevice();
        _width = width;
        _height = height;
        _pixelCount = (ulong)width * (ulong)height;
        ReleaseFrameBuffers();
        CreateHostBuffer(_pixelCount * 12, BufferUsageFlags.StorageBufferBit, out _accumulationBuffer, out _accumulationMemory);
        CreateHostBuffer(_pixelCount * 4, BufferUsageFlags.StorageBufferBit, out _displayBuffer, out _displayMemory, out _displayMapped);
        UpdateDescriptorSet();
    }

    public void UploadScene(CompiledScene scene)
    {
        EnsureDevice();
        ReleaseSceneBuffers();
        UploadBuffer(scene.Triangles.AsSpan(), BufferUsageFlags.StorageBufferBit, out _trianglesBuffer, out _trianglesMemory);
        UploadBuffer(scene.Materials.AsSpan(), BufferUsageFlags.StorageBufferBit, out _materialsBuffer, out _materialsMemory);
        var lights = scene.Lights.IsEmpty ? [default(GpuLight)] : scene.Lights.AsSpan();
        UploadBuffer(lights, BufferUsageFlags.StorageBufferBit, out _lightsBuffer, out _lightsMemory);
        var bvh = scene.BvhNodes.IsEmpty
            ? new[] { default(VulkanGpuBvhNode) }
            : scene.BvhNodes.Select(VulkanGpuBvhNode.From).ToArray();
        UploadBuffer(bvh.AsSpan(), BufferUsageFlags.StorageBufferBit, out _bvhBuffer, out _bvhMemory);
        var order = scene.TriangleOrder.IsEmpty ? new[] { 0 } : scene.TriangleOrder.ToArray();
        UploadBuffer(order.AsSpan(), BufferUsageFlags.StorageBufferBit, out _orderBuffer, out _orderMemory);
        UpdateDescriptorSet();
    }

    public void ClearAccumulation()
    {
        EnsureDevice();
        if (_accumulationBuffer.Handle == 0)
        {
            return;
        }

        _vk.GetBufferMemoryRequirements(_device, _accumulationBuffer, out var req);
        unsafe
        {
            void* mapped = null;
            _vk.MapMemory(_device, _accumulationMemory, 0, req.Size, 0, &mapped);
            new Span<byte>(mapped, (int)req.Size).Clear();
            _vk.UnmapMemory(_device, _accumulationMemory);
        }

        ClearDisplay();
    }

    public void Dispatch(CameraSnapshot camera, int sampleIndex, int lightCount, int bvhRootIndex)
    {
        EnsureDevice();
        var uniform = VulkanFrameUniform.From(camera, _width, _height, sampleIndex, lightCount, bvhRootIndex);
        System.Buffer.MemoryCopy(&uniform, _uniformMapped, sizeof(VulkanFrameUniform), sizeof(VulkanFrameUniform));

        _vk.WaitForFences(_device, 1, in _fence, true, ulong.MaxValue);
        _vk.ResetFences(_device, 1, in _fence);
        _vk.ResetCommandBuffer(_commandBuffer, 0);

        var begin = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };
        _vk.BeginCommandBuffer(_commandBuffer, in begin);
        _vk.CmdBindPipeline(_commandBuffer, PipelineBindPoint.Compute, _pipeline);
        fixed (DescriptorSet* descriptorSet = &_descriptorSet)
        {
            _vk.CmdBindDescriptorSets(_commandBuffer, PipelineBindPoint.Compute, _pipelineLayout, 0, 1, descriptorSet, 0, null);
        }
        var groupCount = (uint)((_width * _height + 255) / 256);
        _vk.CmdDispatch(_commandBuffer, groupCount, 1, 1);
        _vk.EndCommandBuffer(_commandBuffer);

        var submit = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
        };
        fixed (CommandBuffer* cmd = &_commandBuffer)
        {
            submit.PCommandBuffers = cmd;
            _vk.QueueSubmit(_computeQueue, 1, in submit, _fence);
        }
        _vk.WaitForFences(_device, 1, in _fence, true, ulong.MaxValue);
    }

    public void ReadDisplayToCpu(Span<Rgba32> pixels)
    {
        if (_displayMapped == null || pixels.Length == 0)
        {
            return;
        }

        var src = (uint*)_displayMapped;
        for (var i = 0; i < pixels.Length; i++)
        {
            var packed = src[i];
            pixels[i] = new Rgba32(
                (byte)(packed & 0xFF),
                (byte)((packed >> 8) & 0xFF),
                (byte)((packed >> 16) & 0xFF),
                (byte)((packed >> 24) & 0xFF));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_device.Handle == 0)
        {
            _vk.Dispose();
            return;
        }

        _vk.DeviceWaitIdle(_device);
        ReleaseSceneBuffers();
        ReleaseFrameBuffers();
        if (_uniformBuffer.Handle != 0)
        {
            _vk.UnmapMemory(_device, _uniformMemory);
            _vk.DestroyBuffer(_device, _uniformBuffer, null);
            _vk.FreeMemory(_device, _uniformMemory, null);
        }

        if (_pipeline.Handle != 0)
        {
            _vk.DestroyPipeline(_device, _pipeline, null);
        }

        if (_pipelineLayout.Handle != 0)
        {
            _vk.DestroyPipelineLayout(_device, _pipelineLayout, null);
        }

        if (_descriptorPool.Handle != 0)
        {
            _vk.DestroyDescriptorPool(_device, _descriptorPool, null);
        }

        if (_descriptorSetLayout.Handle != 0)
        {
            _vk.DestroyDescriptorSetLayout(_device, _descriptorSetLayout, null);
        }

        if (_shaderModule.Handle != 0)
        {
            _vk.DestroyShaderModule(_device, _shaderModule, null);
        }

        if (_fence.Handle != 0)
        {
            _vk.DestroyFence(_device, _fence, null);
        }

        if (_commandPool.Handle != 0)
        {
            _vk.DestroyCommandPool(_device, _commandPool, null);
        }

        _vk.DestroyDevice(_device, null);
        _vk.DestroyInstance(_instance, null);
        _vk.Dispose();
    }

    private void EnsureDevice()
    {
        if (_device.Handle == 0)
        {
            throw new InvalidOperationException("Vulkan device not initialized.");
        }
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
        if (_vk.CreateInstance(in createInfo, null, out _instance) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan CreateInstance failed.");
        }
    }

    private PhysicalDevice PickPhysicalDevice()
    {
        uint count = 0;
        _vk.EnumeratePhysicalDevices(_instance, ref count, null);
        if (count == 0)
        {
            throw new InvalidOperationException("No Vulkan physical devices.");
        }

        var devices = new PhysicalDevice[count];
        fixed (PhysicalDevice* pDevices = devices)
        {
            _vk.EnumeratePhysicalDevices(_instance, ref count, pDevices);
        }

        foreach (var device in devices)
        {
            if (FindComputeQueueFamily(device, out _))
            {
                _vk.GetPhysicalDeviceProperties(device, out var props);
                if (props.DeviceType == PhysicalDeviceType.DiscreteGpu)
                {
                    return device;
                }
            }
        }

        foreach (var device in devices)
        {
            if (FindComputeQueueFamily(device, out _))
            {
                return device;
            }
        }

        throw new InvalidOperationException("No Vulkan device with compute queue.");
    }

    private bool FindComputeQueueFamily(PhysicalDevice device, out uint index)
    {
        uint count = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, null);
        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* pFamilies = families)
        {
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, pFamilies);
        }

        for (uint i = 0; i < families.Length; i++)
        {
            if ((families[i].QueueFlags & QueueFlags.ComputeBit) != 0)
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
        FindComputeQueueFamily(_physicalDevice, out _computeQueueFamily);
        var priority = 1f;
        var queueCreate = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = _computeQueueFamily,
            QueueCount = 1,
            PQueuePriorities = &priority,
        };
        var deviceCreate = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreate,
        };
        if (_vk.CreateDevice(_physicalDevice, in deviceCreate, null, out _device) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan CreateDevice failed.");
        }

        _vk.GetDeviceQueue(_device, _computeQueueFamily, 0, out _computeQueue);
    }

    private void CreateCommandResources()
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _computeQueueFamily,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
        };
        if (_vk.CreateCommandPool(_device, in poolInfo, null, out _commandPool) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan CreateCommandPool failed.");
        }

        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        if (_vk.AllocateCommandBuffers(_device, in allocInfo, out _commandBuffer) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan AllocateCommandBuffers failed.");
        }

        var fenceInfo = new FenceCreateInfo { SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.SignaledBit };
        if (_vk.CreateFence(_device, in fenceInfo, null, out _fence) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan CreateFence failed.");
        }
    }

    private void CreatePipeline()
    {
        var spirv = VulkanShaderCompiler.GetPathTraceSpirv();
        fixed (byte* code = spirv)
        {
            var moduleInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirv.Length,
                PCode = (uint*)code,
            };
            if (_vk.CreateShaderModule(_device, in moduleInfo, null, out _shaderModule) != Result.Success)
            {
                throw new InvalidOperationException("Vulkan CreateShaderModule failed.");
            }
        }

        CreateDescriptorSetLayout();
        var layoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
        };
        fixed (DescriptorSetLayout* setLayout = &_descriptorSetLayout)
        {
            layoutInfo.PSetLayouts = setLayout;
            if (_vk.CreatePipelineLayout(_device, in layoutInfo, null, out _pipelineLayout) != Result.Success)
            {
                throw new InvalidOperationException("Vulkan CreatePipelineLayout failed.");
            }
        }

        var stage = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            Module = _shaderModule,
            PName = (byte*)Silk.NET.Core.Native.SilkMarshal.StringToPtr("main"),
        };
        var pipelineInfo = new ComputePipelineCreateInfo
        {
            SType = StructureType.ComputePipelineCreateInfo,
            Stage = stage,
            Layout = _pipelineLayout,
        };
        if (_vk.CreateComputePipelines(_device, default, 1, in pipelineInfo, null, out _pipeline) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan CreateComputePipelines failed.");
        }

        CreateHostBuffer((ulong)sizeof(VulkanFrameUniform), BufferUsageFlags.UniformBufferBit, out _uniformBuffer, out _uniformMemory, out _uniformMapped);
    }

    private void CreateDescriptorSetLayout()
    {
        var bindings = stackalloc DescriptorSetLayoutBinding[8];
        for (uint i = 0; i < 8; i++)
        {
            bindings[(int)i] = new DescriptorSetLayoutBinding
            {
                Binding = i,
                DescriptorType = i == 0 ? DescriptorType.UniformBuffer : DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ComputeBit,
            };
        }

        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 8,
            PBindings = bindings,
        };
        if (_vk.CreateDescriptorSetLayout(_device, in layoutInfo, null, out _descriptorSetLayout) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan CreateDescriptorSetLayout failed.");
        }
    }

    private void CreateDescriptorPoolAndSet()
    {
        var poolSizes = stackalloc DescriptorPoolSize[]
        {
            new() { Type = DescriptorType.UniformBuffer, DescriptorCount = 1 },
            new() { Type = DescriptorType.StorageBuffer, DescriptorCount = 7 },
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 2,
            PPoolSizes = poolSizes,
            MaxSets = 1,
        };
        if (_vk.CreateDescriptorPool(_device, in poolInfo, null, out _descriptorPool) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan CreateDescriptorPool failed.");
        }

        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = 1,
        };
        fixed (DescriptorSetLayout* setLayout = &_descriptorSetLayout)
        {
            allocInfo.PSetLayouts = setLayout;
            if (_vk.AllocateDescriptorSets(_device, in allocInfo, out _descriptorSet) != Result.Success)
            {
                throw new InvalidOperationException("Vulkan AllocateDescriptorSets failed.");
            }
        }
    }

    private void UpdateDescriptorSet()
    {
        if (_descriptorSet.Handle == 0)
        {
            return;
        }

        var infos = stackalloc DescriptorBufferInfo[8];
        infos[0] = CreateBufferInfo(_uniformBuffer, (ulong)sizeof(VulkanFrameUniform));
        infos[1] = CreateBufferInfo(_accumulationBuffer, GetBufferSize(_accumulationBuffer));
        infos[2] = CreateBufferInfo(_displayBuffer, GetBufferSize(_displayBuffer));
        infos[3] = CreateBufferInfo(_trianglesBuffer, GetBufferSize(_trianglesBuffer));
        infos[4] = CreateBufferInfo(_materialsBuffer, GetBufferSize(_materialsBuffer));
        infos[5] = CreateBufferInfo(_lightsBuffer, GetBufferSize(_lightsBuffer));
        infos[6] = CreateBufferInfo(_bvhBuffer, GetBufferSize(_bvhBuffer));
        infos[7] = CreateBufferInfo(_orderBuffer, GetBufferSize(_orderBuffer));

        var writes = stackalloc WriteDescriptorSet[8];
        var writeCount = 0;
        for (var i = 0; i < 8; i++)
        {
            if (infos[i].Buffer.Handle == 0)
            {
                continue;
            }

            writes[writeCount] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _descriptorSet,
                DstBinding = (uint)i,
                DescriptorCount = 1,
                DescriptorType = i == 0 ? DescriptorType.UniformBuffer : DescriptorType.StorageBuffer,
                PBufferInfo = &infos[i],
            };
            writeCount++;
        }

        if (writeCount > 0)
        {
            _vk.UpdateDescriptorSets(_device, (uint)writeCount, writes, 0, null);
        }
    }

    private static DescriptorBufferInfo CreateBufferInfo(Silk.NET.Vulkan.Buffer buffer, ulong range) =>
        new()
        {
            Buffer = buffer,
            Offset = 0,
            Range = range == 0 ? Vk.WholeSize : range,
        };

    private ulong GetBufferSize(Silk.NET.Vulkan.Buffer buffer)
    {
        if (buffer.Handle == 0)
        {
            return 0;
        }

        _vk.GetBufferMemoryRequirements(_device, buffer, out var req);
        return req.Size;
    }

    private void UploadBuffer<T>(ReadOnlySpan<T> data, BufferUsageFlags usage, out Silk.NET.Vulkan.Buffer buffer, out DeviceMemory memory)
        where T : unmanaged
    {
        if (data.IsEmpty)
        {
            buffer = default;
            memory = default;
            return;
        }

        var size = (ulong)data.Length * (ulong)sizeof(T);
        CreateDeviceLocalBuffer(size, usage, out buffer, out memory);
        void* mapped = null;
        var stagingBuffer = default(Silk.NET.Vulkan.Buffer);
        var stagingMemory = default(DeviceMemory);
        CreateHostBuffer(size, BufferUsageFlags.TransferSrcBit, out stagingBuffer, out stagingMemory, out mapped);
        data.CopyTo(new Span<T>(mapped, data.Length));
        CopyBuffer(stagingBuffer, buffer, size);
        _vk.DestroyBuffer(_device, stagingBuffer, null);
        _vk.FreeMemory(_device, stagingMemory, null);
    }

    private void CopyBuffer(Silk.NET.Vulkan.Buffer src, Silk.NET.Vulkan.Buffer dst, ulong size)
    {
        var cmdBegin = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        _vk.BeginCommandBuffer(_commandBuffer, in cmdBegin);
        var copy = new BufferCopy { Size = size };
        _vk.CmdCopyBuffer(_commandBuffer, src, dst, 1, in copy);
        _vk.EndCommandBuffer(_commandBuffer);
        var submit = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
        };
        fixed (CommandBuffer* cmd = &_commandBuffer)
        {
            submit.PCommandBuffers = cmd;
            _vk.QueueSubmit(_computeQueue, 1, in submit, default);
        }
        _vk.QueueWaitIdle(_computeQueue);
    }

    private void CreateHostBuffer(
        ulong size,
        BufferUsageFlags usage,
        out Silk.NET.Vulkan.Buffer buffer,
        out DeviceMemory memory,
        out void* mapped)
    {
        mapped = null;
        CreateBuffer(size, usage, out buffer);
        AllocateMemory(buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out memory);
        _vk.BindBufferMemory(_device, buffer, memory, 0);
        if ((usage & BufferUsageFlags.StorageBufferBit) != 0 || (usage & BufferUsageFlags.UniformBufferBit) != 0)
        {
            void* p = null;
            _vk.MapMemory(_device, memory, 0, size, 0, &p);
            mapped = p;
        }
    }

    private void CreateHostBuffer(ulong size, BufferUsageFlags usage, out Silk.NET.Vulkan.Buffer buffer, out DeviceMemory memory) =>
        CreateHostBuffer(size, usage, out buffer, out memory, out _);

    private void CreateDeviceLocalBuffer(ulong size, BufferUsageFlags usage, out Silk.NET.Vulkan.Buffer buffer, out DeviceMemory memory)
    {
        CreateBuffer(size, usage | BufferUsageFlags.TransferDstBit, out buffer);
        AllocateMemory(buffer, MemoryPropertyFlags.DeviceLocalBit, out memory);
        _vk.BindBufferMemory(_device, buffer, memory, 0);
    }

    private void CreateBuffer(ulong size, BufferUsageFlags usage, out Silk.NET.Vulkan.Buffer buffer)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };
        if (_vk.CreateBuffer(_device, in bufferInfo, null, out buffer) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan CreateBuffer failed.");
        }
    }

    private void AllocateMemory(Silk.NET.Vulkan.Buffer buffer, MemoryPropertyFlags properties, out DeviceMemory memory)
    {
        _vk.GetBufferMemoryRequirements(_device, buffer, out var requirements);
        var memoryType = FindMemoryType(requirements.MemoryTypeBits, properties);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = memoryType,
        };
        if (_vk.AllocateMemory(_device, in allocInfo, null, out memory) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan AllocateMemory failed.");
        }
    }

    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out var memProps);
        for (uint i = 0; i < memProps.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << (int)i)) != 0 &&
                (memProps.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }

        throw new InvalidOperationException("Vulkan memory type not found.");
    }

    private void ClearDisplay()
    {
        if (_displayMapped == null)
        {
            return;
        }

        new Span<byte>(_displayMapped, (int)(_pixelCount * 4)).Clear();
    }

    private void ReleaseFrameBuffers()
    {
        if (_accumulationBuffer.Handle != 0)
        {
            _vk.DestroyBuffer(_device, _accumulationBuffer, null);
            _vk.FreeMemory(_device, _accumulationMemory, null);
            _accumulationBuffer = default;
        }

        if (_displayBuffer.Handle != 0)
        {
            _vk.UnmapMemory(_device, _displayMemory);
            _vk.DestroyBuffer(_device, _displayBuffer, null);
            _vk.FreeMemory(_device, _displayMemory, null);
            _displayBuffer = default;
            _displayMapped = null;
        }
    }

    private void ReleaseSceneBuffers()
    {
        DestroyBuffer(ref _trianglesBuffer, ref _trianglesMemory);
        DestroyBuffer(ref _materialsBuffer, ref _materialsMemory);
        DestroyBuffer(ref _lightsBuffer, ref _lightsMemory);
        DestroyBuffer(ref _bvhBuffer, ref _bvhMemory);
        DestroyBuffer(ref _orderBuffer, ref _orderMemory);
    }

    private void DestroyBuffer(ref Silk.NET.Vulkan.Buffer buffer, ref DeviceMemory memory)
    {
        if (buffer.Handle == 0)
        {
            return;
        }

        _vk.DestroyBuffer(_device, buffer, null);
        _vk.FreeMemory(_device, memory, null);
        buffer = default;
        memory = default;
    }
}
