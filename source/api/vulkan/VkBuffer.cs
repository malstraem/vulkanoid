using System.Diagnostics;
using System.Runtime.InteropServices;

using Buffer = Silk.NET.Vulkan.Buffer;

namespace Vulkanoid.Vulkan;

[Handle<Buffer>]
public partial class VkBuffer : DeviceBuffer, IDisposable
{
    internal VkMemory memory;

    public VkBuffer(Buffer handle, VkMemory memory, VkDevice device) : this(handle, device)
    {
        device.BindBufferMemory(handle, memory);

        this.memory = memory;
    }

    public void Load<T>(T[] data)
    {
        ulong size = (ulong)(Marshal.SizeOf<T>() * data.Length);

        Debug.Assert(size == memory.Size);

        unsafe
        {
            void* target = (void*)memory.Map();

            fixed (T* dataPtr = data)
                System.Buffer.MemoryCopy(dataPtr, target, size, size);

            memory.Unmap();
        }
    }

    public void Load<T>(T data)
    {
        ulong size = (ulong)Marshal.SizeOf<T>();

        Debug.Assert(size == memory.Size);

        unsafe
        {
            void* target = (void*)memory.Map();

            System.Buffer.MemoryCopy(&data, target, size, size);

            memory.Unmap();
        }
    }

    public void CopyTo(VkBuffer other, VkCommandPool commandPool)
    {
        using var commandBuffer = commandPool.CreateCommandBuffer();

        commandBuffer.OneTimeSubmit(x => x.CopyBuffer(this, other, new BufferCopy(0ul, 0ul, size)));
    }

    public void Dispose() => throw new NotImplementedException();
}