namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    public VkFence CreateFence(in FenceCreateInfo info)
    {
        unsafe
        {
            vk.CreateFence(handle, info, null, out var fenceHandle).Check();
            return new VkFence(fenceHandle, this);
        }
    }

    public VkSemaphore CreateSemaphore(in SemaphoreCreateInfo info)
    {
        unsafe
        {
            vk.CreateSemaphore(handle, info, null, out var semaphore).Check();
            return new VkSemaphore(semaphore, this);
        }
    }

    public void WaitIdle() => vk.DeviceWaitIdle(handle);

    internal void WaitForFence(in Fence fence, ulong timeout) => vk.WaitForFences(handle, 1u, fence, true, timeout).Check();

    internal void ResetFence(in Fence fence) => vk.ResetFences(handle, 1u, fence).Check();
}
