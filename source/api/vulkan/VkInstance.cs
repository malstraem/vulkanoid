using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Vulkanoid.Vulkan;

public sealed class VkInstance
{
    private readonly Vk vk;

    private readonly KhrSurface surfaceExtension;

    internal readonly Instance handle;

    private QueueFamilies FindQueueFamilies(PhysicalDevice device, SurfaceKHR? surface)
    {
        uint queueFamilyCount = 0;
        var queueFamilies = new QueueFamilies();

        unsafe
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, null);

            var queueFamilyProperties = new Span<QueueFamilyProperties>(new QueueFamilyProperties[queueFamilyCount]);
            vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilyProperties);

            for (uint i = 0u; i < queueFamilyCount; i++)
            {
                if (queueFamilyProperties[(int)i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                {
                    queueFamilies.GraphicsIndex = i;
                    break;
                }
            }

            if (surface is not null)
            {
                for (uint i = 0u; i < queueFamilyCount; i++)
                {
                    surfaceExtension.GetPhysicalDeviceSurfaceSupport(device, i, surface!.Value, out var presentSupported);

                    if (presentSupported)
                    {
                        queueFamilies.PresentIndex = i;
                        break;
                    }
                }
            }
        }
        return queueFamilies;
    }

    private SwapchainSupport GetSwapchainSupport(PhysicalDevice device, SurfaceKHR surface)
    {
        surfaceExtension.GetPhysicalDeviceSurfaceCapabilities(device, surface, out var surfaceCapabilities);

        uint formatCount = 0u, presentModeCount = 0u;

        unsafe
        {
            surfaceExtension.GetPhysicalDeviceSurfaceFormats(device, surface, &formatCount, null);

            Span<SurfaceFormatKHR> formats = stackalloc SurfaceFormatKHR[(int)formatCount];

            surfaceExtension.GetPhysicalDeviceSurfaceFormats(device, surface, &formatCount, formats);

            surfaceExtension.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModeCount, null);

            Span<PresentModeKHR> presentModes = stackalloc PresentModeKHR[(int)presentModeCount];

            surfaceExtension.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModeCount, presentModes);

            return new(surfaceCapabilities, formats.ToArray(), presentModes.ToArray());
        }
    }

    public VkInstance(Vk vk)
    {
        this.vk = vk;

        unsafe
        {
            uint extensionCount;
            vk.EnumerateInstanceExtensionProperties((byte*)null, &extensionCount, null);

            var extensionProperties = stackalloc ExtensionProperties[(int)extensionCount];
            vk.EnumerateInstanceExtensionProperties((byte*)null, &extensionCount, extensionProperties);

            byte** extensionNames = stackalloc byte*[(int)extensionCount];

            for (int i = 0; i < extensionCount; i++)
                extensionNames[i] = extensionProperties[i].ExtensionName;

            var appInfo = new ApplicationInfo(apiVersion: Vk.Version11);
            var createInfo = new InstanceCreateInfo(pApplicationInfo: &appInfo, ppEnabledExtensionNames: extensionNames, enabledExtensionCount: extensionCount);

            vk.CreateInstance(in createInfo, null, out handle);

            vk.CurrentInstance = handle;
        }

        IsPresentSupported = vk.TryGetInstanceExtension(handle, out surfaceExtension);
    }

    public static implicit operator Instance(VkInstance resource) => resource.handle;

    public IEnumerable<VkPhysicalDevice> GetPhysicalDevices(IVkSurface surface)
    {
        unsafe
        {
            var surfaceHandle = surface.Create<AllocationCallbacks>(handle.ToHandle(), null).ToSurface();

            if (!IsPresentSupported & surface is not null)
                throw new NotSupportedException("Presentation requested but not supported");

            var deviceHandles = vk.GetPhysicalDevices(handle);

            if (deviceHandles.Count == 0)
                throw new NotSupportedException("GPU with Vulkan support not found");

            var devices = new List<VkPhysicalDevice>();

            foreach (var deviceHandle in deviceHandles)
            {
                var queueFamilies = FindQueueFamilies(deviceHandle, surfaceHandle);

                SwapchainSupport swapchainSupport = default;

                if (surface is not null && queueFamilies.PresentIndex is null)
                    continue;

                if (surface is not null)
                    swapchainSupport = GetSwapchainSupport(deviceHandle, surfaceHandle);

                devices.Add(new VkPhysicalDevice(deviceHandle, queueFamilies, swapchainSupport, vk));
            }

            return devices;
        }
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