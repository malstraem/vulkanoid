using Silk.NET.Vulkan.Extensions.KHR;

namespace Vulkanoid.Vulkan;

public record struct QueueFamilies(uint Count, uint Graphics, uint Transfer);

public record struct SwapchainSupport(SurfaceCapabilitiesKHR Capabilities, SurfaceFormatKHR[] Formats, PresentModeKHR[] PresentModes);

public sealed class VkPhysicalDevice
{
    private readonly Vk vk;

    private readonly PhysicalDevice handle;

    internal readonly QueueFamilies queueFamilies;

    private Format FindSupportedFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var candidate in candidates)
        {
            vk.GetPhysicalDeviceFormatProperties(handle, candidate, out var properties);

            if (tiling == ImageTiling.Linear && (properties.LinearTilingFeatures & features) == features
                || tiling == ImageTiling.Optimal && (properties.OptimalTilingFeatures & features) == features)
                return candidate;
        }
        throw new Exception("Supported format not found");
    }

    private QueueFamilies FindQueueFamilies()
    {
        uint queueFamilyCount = 0;
        QueueFamilies queueFamilies = default;
        QueueFamilyProperties[] queueFamilyProperties;

        unsafe
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(handle, ref queueFamilyCount, null);

            queueFamilyProperties = new QueueFamilyProperties[queueFamilyCount];
            vk.GetPhysicalDeviceQueueFamilyProperties(handle, &queueFamilyCount, queueFamilyProperties);
        }

        queueFamilies.Count = queueFamilyCount;

        for (uint i = 0u; i < queueFamilyCount; i++)
        {
            if (queueFamilyProperties[(int)i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                queueFamilies.Graphics = i;
                queueFamilies.Transfer = i;
                break;
            }
        }

        for (uint i = 0u; i < queueFamilyCount; i++)
        {
            var flags = queueFamilyProperties[(int)i].QueueFlags;
            if (flags.HasFlag(QueueFlags.TransferBit) && !flags.HasFlag(QueueFlags.GraphicsBit))
            {
                queueFamilies.Transfer = i;
                break;
            }
        }

        return queueFamilies;
    }

    internal uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        vk.GetPhysicalDeviceMemoryProperties(handle, out var memoryProperties);

        for (int i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & 1u << i) != 0 && (memoryProperties.MemoryTypes[i].PropertyFlags & properties) != 0)
                return (uint)i;
        }

        throw new Exception("Suitable memory type not found");
    }

    public VkPhysicalDevice(PhysicalDevice handle, Vk vk)
    {
        this.vk = vk;
        this.handle = handle;

        DepthFormat = FindSupportedFormat(new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint },
            ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);

        queueFamilies = FindQueueFamilies();
    }

    public static implicit operator PhysicalDevice(VkPhysicalDevice resource) => resource.handle;

    public Device CreateDevice()
    {
        float queuePriority = 1f;

        unsafe
        {
            var queueCreateInfos = stackalloc DeviceQueueCreateInfo[(int)queueFamilies.Count];

            for (uint i = 0; i < queueFamilies.Count; i++)
                queueCreateInfos[i] = new DeviceQueueCreateInfo(queueFamilyIndex: i, queueCount: 1u, pQueuePriorities: &queuePriority);

            PhysicalDeviceFeatures deviceFeatures = default;

            string[] extensions = { KhrSwapchain.ExtensionName };

            var createInfo = new DeviceCreateInfo(
                pQueueCreateInfos: queueCreateInfos,
                queueCreateInfoCount: queueFamilies.Count,
                pEnabledFeatures: &deviceFeatures,
                ppEnabledExtensionNames: (byte**)SilkMarshal.StringArrayToPtr(extensions),
                enabledExtensionCount: (uint)extensions.Length);

            var result = vk.CreateDevice(handle, createInfo, null, out var deviceHandle);

            return deviceHandle;
        }
    }

    public Format DepthFormat { get; private set; }
}