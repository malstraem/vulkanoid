using Buffer = Silk.NET.Vulkan.Buffer;

namespace Vulkanoid.Vulkan;

[Handle<Buffer>]
public sealed partial class VkBuffer : DeviceBuffer, IDisposable
{
    public void Dispose() => throw new NotImplementedException();
}