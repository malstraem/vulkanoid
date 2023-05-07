namespace Vulkanoid.Vulkan;

public sealed class VkDevice : GraphicsDevice
{
    private readonly Device handle;

    internal Vk vk = Vk.GetApi();

    ~VkDevice()
    {
        unsafe
        {
            vk.DestroyDevice(handle, null);
        }
    }
}