namespace Vulkanoid.Vulkan;

public record struct QueueFamilies(uint? GraphicsIndex, uint? PresentIndex)
{
    public bool IsComplete => GraphicsIndex.HasValue && PresentIndex.HasValue;
}

public sealed class VkPhysicalDevice
{
    private readonly PhysicalDevice handle;

    private readonly QueueFamilies queueFamilies;

    private readonly Vk vk;

    public VkPhysicalDevice(PhysicalDevice handle, QueueFamilies queueFamilies, Vk vk)
    {
        this.handle = handle;
        this.queueFamilies = queueFamilies;
        this.vk = vk;
    }

    public Device CreateDevice()
    {
        float queuePriority = 1f;

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

            var deviceFeatures = new PhysicalDeviceFeatures(samplerAnisotropy: true); 

            fixed (DeviceQueueCreateInfo* queueCreateInfoPtr = queueCreateInfos)
            {
                var createInfo = new DeviceCreateInfo(
                    pQueueCreateInfos: queueCreateInfoPtr,
                    queueCreateInfoCount: (uint)queueCreateInfos.Length,
                    pEnabledFeatures: &deviceFeatures);

                vk.CreateDevice(handle, createInfo, null, out var deviceHandle);

                return deviceHandle;
            }
        }
    }
}