namespace Vulkanoid.Vulkan;

public sealed class VkDevice : GraphicsDevice
{
    private readonly Device handle;

    private readonly VkInstance instance;

    private readonly VkPhysicalDevice physicalDevice;

    internal Vk vk = Vk.GetApi();
    
    public VkDevice(SurfaceKHR? surface)
    {
        instance = new VkInstance(vk);

        physicalDevice = instance.GetPhysicalDevices(surface).First(); // todo: allow to list and pick devices

        handle = physicalDevice.CreateDevice();
    }

    ~VkDevice()
    {
        unsafe
        {
            vk.DestroyDevice(handle, null);
        }
    }
}