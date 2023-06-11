namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    public VkFence CreateFence(in FenceCreateInfo info)
    {
        unsafe
        {
            vk.CreateFence(handle, info, null, out var fenceHandle);
            return new VkFence(fenceHandle, this);
        }
    }

    public VkSemaphore CreateSemaphore(in SemaphoreCreateInfo info)
    {
        unsafe
        {
            vk.CreateSemaphore(handle, info, null, out var semaphore);
            return new VkSemaphore(semaphore, this);
        }
    }

    public void WaitIdle() => vk.DeviceWaitIdle(handle);

    internal void WaitForFence(in Fence fence, ulong timeout)
    {
        var result = vk.WaitForFences(handle, 1u, fence, true, timeout);
    }

    internal void ResetFence(in Fence fence)
    {
        var result = vk.ResetFences(handle, 1u, fence);
    }
}