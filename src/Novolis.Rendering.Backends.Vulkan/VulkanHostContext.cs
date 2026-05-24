using Novolis.Math.Geometry;
using Silk.NET.Vulkan;

namespace Novolis.Rendering.Backends.Vulkan;

/// <summary>Minimal Vulkan device with a host-visible RGBA buffer for presentation bridges.</summary>
internal sealed unsafe class VulkanHostContext : IDisposable
{
    private readonly Vk _vk = Vk.GetApi();
    private Instance _instance;
    private Device _device;
    private PhysicalDevice _physicalDevice;
    private DeviceMemory _memory;
    private Silk.NET.Vulkan.Buffer _buffer;
    private void* _mapped;
    private ulong _capacityBytes;
    private bool _disposed;

    public nint BufferHandle => (nint)_buffer.Handle;

    public void Upload(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();
        var required = (ulong)width * (ulong)height * 4;
        if (_capacityBytes < required)
        {
            RecreateBuffer(required);
        }

        var rowBytes = width * 4;
        var src = pixels;
        var dst = (byte*)_mapped;
        for (var y = 0; y < height; y++)
        {
            var srcRow = y * width;
            var dstRow = (height - 1 - y) * width;
            for (var x = 0; x < width; x++)
            {
                var p = src[srcRow + x];
                var i = (dstRow + x) * 4;
                dst[i] = p.R;
                dst[i + 1] = p.G;
                dst[i + 2] = p.B;
                dst[i + 3] = p.A;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_buffer.Handle != 0)
        {
            _vk.UnmapMemory(_device, _memory);
            _vk.DestroyBuffer(_device, _buffer, null);
            _vk.FreeMemory(_device, _memory, null);
        }

        if (_device.Handle != 0)
        {
            _vk.DestroyDevice(_device, null);
        }

        if (_instance.Handle != 0)
        {
            _vk.DestroyInstance(_instance, null);
        }

        _vk.Dispose();
    }

    private void EnsureInitialized()
    {
        if (_instance.Handle != 0)
        {
            return;
        }

        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            ApiVersion = Vk.Version12,
            ApplicationVersion = Vk.MakeVersion(1, 0, 0),
            EngineVersion = Vk.MakeVersion(1, 0, 0),
            PApplicationName = (byte*)Silk.NET.Core.Native.SilkMarshal.StringToPtr("Novolis"),
            PEngineName = (byte*)Silk.NET.Core.Native.SilkMarshal.StringToPtr("Novolis.Rendering"),
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

        _physicalDevice = PickPhysicalDevice();
        var queueFamily = FindQueueFamily(_physicalDevice);
        var queuePriority = 1f;
        var queueCreate = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = queueFamily,
            QueueCount = 1,
            PQueuePriorities = &queuePriority,
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
            _vk.GetPhysicalDeviceProperties(device, out var props);
            if (props.DeviceType == PhysicalDeviceType.DiscreteGpu)
            {
                return device;
            }
        }

        return devices[0];
    }

    private uint FindQueueFamily(PhysicalDevice device)
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
            if ((families[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
            {
                return i;
            }
        }

        return 0;
    }

    private void RecreateBuffer(ulong sizeBytes)
    {
        if (_buffer.Handle != 0)
        {
            _vk.UnmapMemory(_device, _memory);
            _vk.DestroyBuffer(_device, _buffer, null);
            _vk.FreeMemory(_device, _memory, null);
        }

        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = sizeBytes,
            Usage = BufferUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive,
        };

        if (_vk.CreateBuffer(_device, in bufferInfo, null, out _buffer) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan CreateBuffer failed.");
        }

        _vk.GetBufferMemoryRequirements(_device, _buffer, out var requirements);
        uint memoryType = FindMemoryType(requirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = memoryType,
        };

        if (_vk.AllocateMemory(_device, in allocInfo, null, out _memory) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan AllocateMemory failed.");
        }

        _vk.BindBufferMemory(_device, _buffer, _memory, 0);
        void* mapped = null;
        if (_vk.MapMemory(_device, _memory, 0, sizeBytes, 0, &mapped) != Result.Success)
        {
            throw new InvalidOperationException("Vulkan MapMemory failed.");
        }

        _mapped = mapped;
        _capacityBytes = sizeBytes;
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
}
