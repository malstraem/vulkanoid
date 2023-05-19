namespace Vulkanoid.Vulkan;

[Handle<Image>]
public sealed partial class VkImage : IDisposable
{
    public void TransitionImageLayout(VkCommandPool commandPool, Format format, ImageLayout oldLayout, ImageLayout newLayout, uint mipLevels)
    {
        using var commandBuffer = commandPool.CreateCommandBuffer();

        unsafe
        {
            var aspect = newLayout == ImageLayout.DepthStencilAttachmentOptimal
                ? (format.HasStencil()
                    ? ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit
                    : ImageAspectFlags.DepthBit)
                : ImageAspectFlags.ColorBit;

            var barrier = new ImageMemoryBarrier(
                oldLayout: oldLayout,
                newLayout: newLayout,
                srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
                dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
                image: handle,
                subresourceRange: new(aspect, 0, mipLevels, 0, 1));

            PipelineStageFlags sourceStage, destinationStage;

            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
            {
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                sourceStage = PipelineStageFlags.TopOfPipeBit;
                destinationStage = PipelineStageFlags.TransferBit;
            }
            else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                sourceStage = PipelineStageFlags.TransferBit;
                destinationStage = PipelineStageFlags.FragmentShaderBit;
            }
            else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit;
                sourceStage = PipelineStageFlags.TopOfPipeBit;
                destinationStage = PipelineStageFlags.EarlyFragmentTestsBit;
            }
            else
            {
                throw new Exception("unsuported layout transition!");
            }

            commandBuffer.PipelineBarrier(sourceStage, destinationStage, barrier);
        }
    }

    public void CopyBufferToImage(VkBuffer buffer, VkCommandPool commandPool, uint width, uint height)
    {
        using var commandBuffer = commandPool.CreateCommandBuffer();

        var region = new BufferImageCopy(0, 0, 0, new(ImageAspectFlags.ColorBit, 0, 0, 1), new(0, 0, 0), new(width, height, 1));

        commandBuffer.CopyBufferToImage(buffer, this, ImageLayout.TransferDstOptimal, region);
    }

    public void Dispose() => throw new NotImplementedException();
}