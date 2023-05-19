namespace Vulkanoid.Vulkan;

[Handle<Queue>]
public sealed partial class VkQueue
{
    public void Submit(in SubmitInfo info, in Fence? fence = null) => device.vk.QueueSubmit(handle, 1u, info, fence ?? default);

    public void WaitIdle() => device.vk.QueueWaitIdle(handle);
}