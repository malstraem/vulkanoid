using System.Diagnostics;
using System.Runtime.CompilerServices;

using Buffer = Silk.NET.Vulkan.Buffer;

namespace Vulkanoid.Vulkan;

[Handle<Buffer>]
public partial class VkBuffer : DeviceBuffer, IDisposable
{
    internal VkMemory memory;

    public VkBuffer(Buffer handle, VkMemory memory, VkDevice device) : this(handle, device)
    {
        size = memory.Size;
        this.memory = memory;

        device.BindBufferMemory(handle, memory);
    }

    public void Upload<T>(Span<T> data)
    {
#if DEBUG
        ulong size = (ulong)(Unsafe.SizeOf<T>() * data.Length);
        Debug.Assert(size <= memory.Size);
#endif
        unsafe
        {
            void* target = (void*)memory.Map();

            fixed (T* dataPtr = data)
                System.Buffer.MemoryCopy(dataPtr, target, size, size);

            memory.Unmap();
        }
    }

    public void Upload<T>(T[] data)
    {
#if DEBUG
        ulong size = (ulong)(Unsafe.SizeOf<T>() * data.Length);
        Debug.Assert(size <= memory.Size);
#endif
        unsafe
        {
            void* target = (void*)memory.Map();

            fixed (T* dataPtr = data)
                System.Buffer.MemoryCopy(dataPtr, target, size, size);

            memory.Unmap();
        }
    }

    public void UploadSingle<T>(T data)
    {
#if DEBUG
        ulong size = (ulong)Unsafe.SizeOf<T>();
        Debug.Assert(size <= memory.Size);
#endif
        unsafe
        {
            void* target = (void*)memory.Map();
            Unsafe.Copy(target, ref data);
            memory.Unmap();
        }
    }

    public void CopyTo(Buffer other, VkCommandPool commandPool)
    {
        using var commandBuffer = commandPool.CreateCommandBuffer();

        commandBuffer.OneTimeSubmit(x => x.CopyBuffer(handle, other, new BufferCopy(0ul, 0ul, size)));
    }

    public void CopyToImage(Image image, VkCommandPool commandPool, uint width, uint height)
    {
        using var commandBuffer = commandPool.CreateCommandBuffer();

        var region = new BufferImageCopy(0, 0, 0, new(ImageAspectFlags.ColorBit, 0, 0, 1), new(0, 0, 0), new(width, height, 1));

        commandBuffer.OneTimeSubmit(x => x.CopyBufferToImage(handle, image, ImageLayout.TransferDstOptimal, region));
    }

    public void Dispose() => throw new NotImplementedException();
}