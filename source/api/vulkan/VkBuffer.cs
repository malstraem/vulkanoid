using System.Diagnostics;
using System.Runtime.CompilerServices;

using Buffer = Silk.NET.Vulkan.Buffer;

namespace Vulkanoid.Vulkan;

[Handle<Buffer>]
public partial class VkBuffer : IBuffer, IDisposable
{
    internal VkMemory memory;

    public VkBuffer(Buffer handle, VkMemory memory, VkDevice device) : this(handle, device)
    {
        this.memory = memory;

        device.BindBufferMemory(handle, memory);
    }

    public void Upload<T>(Span<T> data)
    {
#if DEBUG
        Debug.Assert((ulong)(Unsafe.SizeOf<T>() * data.Length) <= memory.Size);
#endif
        unsafe
        {
            void* target = (void*)memory.Map();

            fixed (T* dataPtr = data)
                System.Buffer.MemoryCopy(dataPtr, target, memory.Size, memory.Size);

            memory.Unmap();
        }
    }

    public void Upload<T>(T[] data)
    {
#if DEBUG
        Debug.Assert((ulong)(Unsafe.SizeOf<T>() * data.Length) <= memory.Size);
#endif
        unsafe
        {
            void* target = (void*)memory.Map();

            fixed (T* dataPtr = data)
                System.Buffer.MemoryCopy(dataPtr, target, memory.Size, memory.Size);

            memory.Unmap();
        }
    }

    public void Upload<T>(T data) where T : unmanaged
    {
#if DEBUG
        Debug.Assert((ulong)Unsafe.SizeOf<T>() <= memory.Size);
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

        commandBuffer.OneTimeSubmit(x => x.CopyBuffer(handle, other, new BufferCopy(0ul, 0ul, memory.Size)));
    }

    public void CopyToImage(Image image, VkCommandPool commandPool, uint width, uint height)
    {
        using var commandBuffer = commandPool.CreateCommandBuffer();

        var region = new BufferImageCopy(0, 0, 0, new(ImageAspectFlags.ColorBit, 0, 0, 1), new(0, 0, 0), new(width, height, 1));

        commandBuffer.OneTimeSubmit(x => x.CopyBufferToImage(handle, image, ImageLayout.TransferDstOptimal, region));
    }

    public void Dispose() => throw new NotImplementedException();

    public ulong Size => memory.Size;
}
