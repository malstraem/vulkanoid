using System.Runtime.CompilerServices;

using Buffer = Silk.NET.Vulkan.Buffer;

namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    public VkBuffer CreateBuffer<T>(BufferUsageFlags usage,
                                    int elementCount = 1,
                                    MemoryPropertyFlags properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                                    BufferCreateFlags create = BufferCreateFlags.None,
                                    SharingMode mode = SharingMode.Exclusive)
    {
        unsafe
        {
            var info = new BufferCreateInfo(flags: create, usage: usage | BufferUsageFlags.TransferDstBit, sharingMode: mode,
                                            size: (ulong)(Unsafe.SizeOf<T>() * elementCount));

            vk.CreateBuffer(handle, in info, null, out var bufferHandle).Check();
            vk.GetBufferMemoryRequirements(handle, bufferHandle, out var memoryRequirements);

            return new VkBuffer(bufferHandle, AllocateMemory(memoryRequirements, properties), this);
        }
    }

    public void DestroyBuffer(Buffer bufferHandle)
    {
        unsafe
        {
            vk.DestroyBuffer(handle, bufferHandle, null);
        }
    }
}
