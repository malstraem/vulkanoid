namespace Vulkanoid.Vulkan;

[Handle<DeviceMemory>]
public sealed partial class VkMemory
{
    public required ulong Size { get; init; }

    public nint Map() => device.MapMemory(handle, Size);

    public void Unmap() => device.UnmapMemory(handle);
}
