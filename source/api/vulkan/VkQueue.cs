namespace Vulkanoid.Vulkan;

[Handle<Queue>]
public sealed partial class VkQueue
{
    internal uint family;

    public VkQueue(Queue handle, VkDevice device, uint family) : this(handle, device) => this.family = family;

    public void Submit(SubmitInfo info, Fence? fence = null)
    {
        var result = device.vk.QueueSubmit(handle, 1u, info, fence ?? default);
    }

    public void WaitIdle()
    {
        var result = device.vk.QueueWaitIdle(handle);
    }
}