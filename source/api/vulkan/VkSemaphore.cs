using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Vulkanoid.Vulkan;

[Handle<Semaphore>]
public sealed partial class VkSemaphore : IDisposable
{
    public void Dispose() => throw new NotImplementedException();
}