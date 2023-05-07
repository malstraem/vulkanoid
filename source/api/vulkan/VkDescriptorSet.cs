namespace Vulkanoid.Vulkan;

[Handle<DescriptorSet>]
public sealed partial class VkDescriptorSet<T> : IDisposable where T : unmanaged
{
    public void Dispose() => throw new NotImplementedException();
}