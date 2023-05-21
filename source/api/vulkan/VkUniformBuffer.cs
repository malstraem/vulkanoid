/*using System.Runtime.InteropServices;

namespace Vulkanoid.Vulkan;

public sealed class VkUniformBuffer<T> : VkBuffer where T : unmanaged
{
    public VkUniformBuffer(VkDevice device)
        : base(device, (ulong)Marshal.SizeOf<T>(), BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit) { }

    public void CopyData(in T sourceData)
    {
        unsafe
        {
            void* data;
            vk.MapMemory(device, memory, 0, size, 0, &data).Check();
            fixed (T* pSourceData = &sourceData)
                System.Buffer.MemoryCopy(pSourceData, data, (long)size, (long)size);
            vk.UnmapMemory(device, memory);
        }
    }
}*/