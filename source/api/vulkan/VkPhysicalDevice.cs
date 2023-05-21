using Silk.NET.Core.Native;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Vulkanoid.Vulkan;

public record struct QueueFamilies(uint GraphicsIndex, uint? PresentIndex);

public record struct SwapchainSupport(SurfaceCapabilitiesKHR Capabilities, SurfaceFormatKHR[] Formats, PresentModeKHR[] PresentModes);

public sealed class VkPhysicalDevice
{
    private readonly Vk vk;

    private readonly PhysicalDevice handle;

    internal readonly QueueFamilies queueFamilies;

    internal readonly SwapchainSupport swapchainSupport;


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

    public VkPhysicalDevice(PhysicalDevice handle, QueueFamilies queueFamilies, SwapchainSupport swapchainSupport, Vk vk)
    {
        this.vk = vk;
        this.handle = handle;
        this.queueFamilies = queueFamilies;
        this.swapchainSupport = swapchainSupport;

        DepthFormat = FindSupportedFormat(new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint },
            ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
    }

    public static implicit operator PhysicalDevice(VkPhysicalDevice resource) => resource.handle;

    internal uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        vk.GetPhysicalDeviceMemoryProperties(handle, out var memoryProperties);

        for (int i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) != 0 && (memoryProperties.MemoryTypes[i].PropertyFlags & properties) != 0)
                return (uint)i;
        }

        throw new Exception("Suitable memory type not found");
    }

    public Device CreateDevice()
    {
        float queuePriority = 1f;
        uint uniqueQueueCount = queueFamilies.GraphicsIndex == queueFamilies.PresentIndex.Value ? 1u : 2u;

        Span<DeviceQueueCreateInfo> queueCreateInfos;

        unsafe
        {
            if (queueFamilies.PresentIndex is not null)
            {
                queueCreateInfos = stackalloc DeviceQueueCreateInfo[2];

                queueCreateInfos[1] = new DeviceQueueCreateInfo(
                    queueFamilyIndex: queueFamilies.PresentIndex,
                    queueCount: 1u,
                    pQueuePriorities: &queuePriority);
            }
            else
            {
                queueCreateInfos = stackalloc DeviceQueueCreateInfo[1];
            }

            queueCreateInfos[0] = new DeviceQueueCreateInfo(
                 queueFamilyIndex: queueFamilies.GraphicsIndex,
                 queueCount: 1u,
                 pQueuePriorities: &queuePriority);

            var deviceFeatures = new PhysicalDeviceFeatures();

            string[] extensions = { KhrSwapchain.ExtensionName };

            fixed (DeviceQueueCreateInfo* queueCreateInfoPtr = queueCreateInfos)
            {
                var createInfo = new DeviceCreateInfo(
                    pQueueCreateInfos: queueCreateInfoPtr,
                    queueCreateInfoCount: uniqueQueueCount,
                    pEnabledFeatures: &deviceFeatures,
                    ppEnabledExtensionNames: (byte**)SilkMarshal.StringArrayToPtr(extensions),
                    enabledExtensionCount: (uint)extensions.Length);

                var result = vk.CreateDevice(handle, createInfo, null, out var deviceHandle);

                return deviceHandle;
            }
        }
    }

    public Format DepthFormat { get; private set; }
}