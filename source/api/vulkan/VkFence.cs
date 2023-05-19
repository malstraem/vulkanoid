namespace Vulkanoid.Vulkan;

[Handle<Fence>]
public sealed partial class VkFence : IDisposable
{
    public void Wait(ulong timeout) => device.WaitForFence(in handle, timeout);

    public void Reset() => device.ResetFence(in handle);

    public void Dispose() => throw new NotImplementedException();
}