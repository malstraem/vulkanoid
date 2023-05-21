namespace Vulkanoid.Vulkan;

[Handle<Queue>]
public sealed partial class VkQueue
{
    public void Submit(SubmitInfo info, Fence? fence = null)
    {
        var result = device.vk.QueueSubmit(handle, 1u, info, fence ?? default);
    }

    public void WaitIdle()
    {
        var result = device.vk.QueueWaitIdle(handle);
    }
}