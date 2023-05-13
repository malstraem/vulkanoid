using Silk.NET.Vulkan.Extensions.KHR;

namespace Vulkanoid.Vulkan;

public sealed class VkInstance
{
    private readonly Vk vk;

    private readonly KhrSurface surfaceExtension;

    private readonly Instance handle;

    private QueueFamilies FindQueueFamilies(PhysicalDevice device, SurfaceKHR? surface)
    {
        var queueFamilies = new QueueFamilies();
        uint queryFamilyCount = 0;

        unsafe
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queryFamilyCount, null);

            var queueFamilyProperties = new Span<QueueFamilyProperties>(new QueueFamilyProperties[queryFamilyCount]);
            vk.GetPhysicalDeviceQueueFamilyProperties(device, &queryFamilyCount, queueFamilyProperties);

            for (uint i = 0u; i < queryFamilyCount; i++)
            {
                if (queueFamilyProperties[(int)i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                {
                    queueFamilies.GraphicsIndex = i;

                    if (surface is null)
                        break;
                }

                surfaceExtension.GetPhysicalDeviceSurfaceSupport(device, i, surface!.Value, out var presentSupported);

                if (presentSupported)
                {
                    queueFamilies.PresentIndex = i;
                    
                    if (queueFamilies.IsComplete)
                        break;
                }
            }
        }
        return queueFamilies;
    }

    public VkInstance(Vk vk)
    {
        this.vk = vk;

        unsafe
        {
            var appInfo = new ApplicationInfo(apiVersion: Vk.Version11);
            var createInfo = new InstanceCreateInfo(pApplicationInfo: &appInfo);

            vk.CreateInstance(in createInfo, null, out handle);

            vk.CurrentInstance = handle;
        }

        IsPresentSupported = vk.TryGetInstanceExtension(handle, out surfaceExtension);
    }

    public IEnumerable<VkPhysicalDevice> GetPhysicalDevices(SurfaceKHR? surface)
    {
        if (!IsPresentSupported)
            throw new NotSupportedException("Presentation requested but not supported");

        var deviceHandles = vk.GetPhysicalDevices(handle);

        if (deviceHandles.Count == 0)
            throw new NotSupportedException("No one GPU with Vulkan was found");

        var devices = new List<VkPhysicalDevice>();

        foreach (var deviceHandle in deviceHandles)
        {
            var queueFamilies = FindQueueFamilies(deviceHandle, surface);

            if (surface is not null && queueFamilies.PresentIndex is null)
                continue;

            devices.Add(new VkPhysicalDevice(deviceHandle, queueFamilies, vk));
        }

        return devices;
    }

    public bool IsPresentSupported { get; private init; }

    ~VkInstance()
    {
        unsafe
        {
            vk.DestroyInstance(handle, null);
        }
    }
}