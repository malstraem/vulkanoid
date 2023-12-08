namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    internal void WaitForFence(Fence fence, ulong timeout) => vk.WaitForFences(handle, 1u, fence, true, timeout).Check();

    internal void ResetFence(Fence fence) => vk.ResetFences(handle, 1u, fence).Check();

    public void WaitIdle() => vk.DeviceWaitIdle(handle);

    public VkFence CreateFence(in FenceCreateInfo info)
    {
        unsafe
        {
            vk.CreateFence(handle, in info, null, out var fenceHandle).Check();
            return new VkFence(fenceHandle, this);
        }
    }

    public VkSemaphore CreateSemaphore(in SemaphoreCreateInfo info)
    {
        unsafe
        {
            vk.CreateSemaphore(handle, in info, null, out var semaphore).Check();
            return new VkSemaphore(semaphore, this);
        }
    }
}
