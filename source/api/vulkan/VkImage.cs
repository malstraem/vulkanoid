namespace Vulkanoid.Vulkan;

[Handle<Image>]
public sealed partial class VkImage : IDisposable
{
    private readonly VkMemory memory;

    internal void TransitionImageLayout(VkCommandPool commandPool, Format format, ImageLayout oldLayout, ImageLayout newLayout, uint mipLevels)
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
                throw new Exception("unsuported layout transition");
            }

            commandBuffer.PipelineBarrier(sourceStage, destinationStage, barrier);
        }
    }

    internal void GenerateMipMaps(int width, int height, uint mipLevels, Format format, VkCommandPool commandPool)
    {
        MipLevels = mipLevels;

        device.vk.GetPhysicalDeviceFormatProperties(device.physicalDevice, format, out var formatProperties);

        if ((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.SampledImageFilterLinearBit) == 0)
            throw new Exception("texture image format does not support linear blitting");

        using var commandBuffer = commandPool.CreateCommandBuffer();

        unsafe
        {
            var barrier = new ImageMemoryBarrier(
                image: handle,
                srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
                dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
                subresourceRange: new(ImageAspectFlags.ColorBit, null, 1, 0, 1));

            for (uint i = 1u; i < mipLevels; i++)
            {
                barrier.SubresourceRange.BaseMipLevel = i - 1;
                barrier.OldLayout = ImageLayout.TransferDstOptimal;
                barrier.NewLayout = ImageLayout.TransferSrcOptimal;
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;

                commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, barrier);

                var blit = new ImageBlit(new(ImageAspectFlags.ColorBit, i - 1u, 0u, 1u), new(ImageAspectFlags.ColorBit, i, 0u, 1u))
                {
                    SrcOffsets = new() { Element0 = new(0, 0, 0), Element1 = new(width, height, 1) },
                    DstOffsets = new() { Element0 = new(0, 0, 0), Element1 = new(width > 1 ? width / 2 : 1, height > 1 ? height / 2 : 1, 1) },
                };

                commandBuffer.BlitImage(handle, ImageLayout.TransferSrcOptimal, handle, ImageLayout.TransferDstOptimal, blit, Filter.Linear);

                barrier.OldLayout = ImageLayout.TransferSrcOptimal;
                barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
                barrier.SrcAccessMask = AccessFlags.TransferReadBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;

                commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, barrier);

                if (width > 1)
                    width /= 2;

                if (height > 1)
                    height /= 2;
            }

            barrier.SubresourceRange.BaseMipLevel = mipLevels - 1;
            barrier.OldLayout = ImageLayout.TransferDstOptimal;
            barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, barrier).Submit();
        }
    }

    public VkImage(Image handle, VkMemory memory, VkDevice device) : this(handle, device)
    {
        this.memory = memory;

        device.BindImageMemory(handle, memory);
    }

    public uint MipLevels { get; set; }

    public required Format Format { get; init; }

    public void Dispose() => throw new NotImplementedException();
}
