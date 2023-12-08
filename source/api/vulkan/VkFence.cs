namespace Vulkanoid.Vulkan;

[Handle<Fence>]
public sealed partial class VkFence : IDisposable
{
    public void Wait(ulong timeout) => device.WaitForFence(handle, timeout);

    public void Reset() => device.ResetFence(handle);

    public void Dispose() => throw new NotImplementedException();
}
