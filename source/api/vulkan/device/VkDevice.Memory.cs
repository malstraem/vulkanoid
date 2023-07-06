using Buffer = Silk.NET.Vulkan.Buffer;

namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    internal VkMemory AllocateMemory(MemoryRequirements requirements, MemoryPropertyFlags properties)
    {
        unsafe
        {
            var allocateInfo = new MemoryAllocateInfo(allocationSize: requirements.Size, memoryTypeIndex: physicalDevice.FindMemoryType(requirements.MemoryTypeBits, properties));

            vk.AllocateMemory(handle, in allocateInfo, null, out var memoryHandle).Check();

            return new VkMemory(memoryHandle, this) { Size = requirements.Size };
        }
    }

    internal nint MapMemory(DeviceMemory memory, ulong size)
    {
        unsafe
        {
            void* data;
            vk.MapMemory(handle, memory, 0u, size, 0u, &data).Check();

            return (nint)data;
        }
    }

    internal void UnmapMemory(DeviceMemory memory) => vk.UnmapMemory(handle, memory);

    internal void BindBufferMemory(Buffer bufferHandle, DeviceMemory memoryHandle) => vk.BindBufferMemory(handle, bufferHandle, memoryHandle, 0).Check();

    internal void BindImageMemory(Image imageHandle, DeviceMemory memoryHandle) => vk.BindImageMemory(handle, imageHandle, memoryHandle, 0u).Check();

    internal void FreeMemory(DeviceMemory memoryHandle)
    {
        unsafe
        {
            vk.FreeMemory(handle, memoryHandle, null);
        }
    }
}
