namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    public VkCommandPool CreateCommandPool()
    {
        unsafe
        {
            var info = new CommandPoolCreateInfo(queueFamilyIndex: 0u);

            vk.CreateCommandPool(handle, info, null, out var poolHandle);

            return new VkCommandPool(poolHandle, this);
        }
    }

    public void DestroyCommandPool(CommandPool poolHandle)
    {
        unsafe
        {
            vk.DestroyCommandPool(handle, poolHandle, null);
        }
    }
}