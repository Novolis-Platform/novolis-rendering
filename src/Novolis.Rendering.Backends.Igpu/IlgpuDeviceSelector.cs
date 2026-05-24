using ILGPU;
using ILGPU.Runtime;

namespace Novolis.Rendering.Backends.Igpu;

/// <summary>Picks an ILGPU device; honors <c>NOVOLIS_ILGPU_DEVICE</c> (cuda, nvidia, cpu, index:N).</summary>
internal static class IlgpuDeviceSelector
{
    public static Device SelectDevice(Context context)
    {
        var devices = context.Devices.ToArray();
        var hint = Environment.GetEnvironmentVariable("NOVOLIS_ILGPU_DEVICE");

        if (!string.IsNullOrWhiteSpace(hint))
        {
            if (TrySelectByHint(devices, hint.Trim(), out var hinted))
            {
                return hinted;
            }
        }

        return SelectDefault(devices, context);
    }

    private static bool TrySelectByHint(Device[] devices, string hint, out Device device)
    {
        if (hint.Equals("cpu", StringComparison.OrdinalIgnoreCase))
        {
            device = devices.First(d => d.AcceleratorType == AcceleratorType.CPU);
            return true;
        }

        if (hint.StartsWith("index:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(hint.AsSpan(6), out var index)
            && index >= 0
            && index < devices.Length)
        {
            device = devices[index];
            return true;
        }

        if (hint is "cuda" or "nvidia")
        {
            var picked = devices.FirstOrDefault(d => d.AcceleratorType == AcceleratorType.Cuda)
                ?? devices.FirstOrDefault(d => NameLooksLikeNvidia(d.Name));
            if (picked is not null)
            {
                device = picked;
                return true;
            }
        }

        device = default!;
        return false;
    }

    private static Device SelectDefault(Device[] devices, Context context)
    {
        var cudaNvidia = devices.FirstOrDefault(d =>
            d.AcceleratorType == AcceleratorType.Cuda && NameLooksLikeNvidia(d.Name));
        if (cudaNvidia is not null)
        {
            return cudaNvidia;
        }

        var anyCuda = devices.FirstOrDefault(d => d.AcceleratorType == AcceleratorType.Cuda);
        if (anyCuda is not null)
        {
            return anyCuda;
        }

        var nvidiaOpenCl = devices.FirstOrDefault(d =>
            d.AcceleratorType == AcceleratorType.OpenCL && NameLooksLikeNvidia(d.Name));
        if (nvidiaOpenCl is not null)
        {
            return nvidiaOpenCl;
        }

        var nonAmd = devices.FirstOrDefault(d =>
            d.AcceleratorType != AcceleratorType.CPU && !NameLooksLikeAmd(d.Name));
        if (nonAmd is not null)
        {
            return nonAmd;
        }

        return context.GetPreferredDevice(preferCPU: false);
    }

    private static bool NameLooksLikeNvidia(string name) =>
        name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)
        || name.Contains("GeForce", StringComparison.OrdinalIgnoreCase)
        || name.Contains("RTX", StringComparison.OrdinalIgnoreCase)
        || name.Contains("GTX", StringComparison.OrdinalIgnoreCase);

    private static bool NameLooksLikeAmd(string name) =>
        name.Contains("AMD", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)
        || name.Contains("gfx", StringComparison.OrdinalIgnoreCase);
}
