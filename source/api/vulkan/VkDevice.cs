using Silk.NET.Vulkan.Extensions.KHR;

namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    private readonly Device handle;

    private readonly KhrSwapchain? swapchainExt;

    internal readonly Vk vk;

    internal readonly VkInstance instance;
    internal readonly VkPhysicalDevice physicalDevice;

    internal readonly VkQueue graphicsQueue;

    private VkQueue GetQueue(uint family) => new(vk.GetDeviceQueue(handle, family, 0u), this, family);

    public VkDevice(Vk vk, string[] extensions)
    {
        this.vk = vk;

        instance = new VkInstance(vk, extensions);

        physicalDevice = instance.GetPhysicalDevices().First(); // todo: allow to list and pick devices
        handle = physicalDevice.CreateDevice();

        graphicsQueue = GetQueue(physicalDevice.queueFamilies.Graphics);

        vk.TryGetDeviceExtension(instance, handle, out swapchainExt);
    }

    ~VkDevice()
    {
        unsafe
        {
            vk.DestroyDevice(handle, null);
        }
    }

    public static implicit operator Device(VkDevice resource) => resource.handle;
}