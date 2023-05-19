/*using System.Runtime.InteropServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using RawImage = SixLabors.ImageSharp.Image;

namespace Vulkanoid.Vulkan;

public sealed class VkTexture : IDisposable
{
    private readonly VkDevice device;

    public VkImage? Image { get; private set; }

    public VkImageView? ImageView { get; private set; }

    public uint MipLevels => mipLevels;

    private readonly uint mipLevels;

    public VkTexture(VkDevice device, VkCommandPool commandPool, Stream imageStream)
    {
        this.device = device;

        Configuration.Default.PreferContiguousImageBuffers = true;

        using var image = RawImage.Load<Rgba32>(imageStream) ?? throw new VkException("failed to load texture image!");

        int width = image.Width;
        int height = image.Height;
        int imageSize = width * height * 4;

        mipLevels = (uint)Math.Floor(Math.Log2(Math.Max(image.Width, image.Height))) + 1;

        unsafe
        {
            VkStagingBuffer<Rgba32>? staging = null;

            image.ProcessPixelRows(accessor =>
            {
                fixed (Rgba32* data = &MemoryMarshal.GetReference(accessor.GetRowSpan(0)))
                    staging = new(device, (ulong)imageSize, data);
            });

            if (staging == null)
                throw new VkException("failed to copy texture!");

            try
            {
                Image = new(device, (uint)image.Width, (uint)image.Height, mipLevels, Format.R8G8B8A8Srgb, ImageTiling.Optimal,
                    ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit);

                Image.TransitionImageLayout(commandPool, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, mipLevels);
                Image.CopyBufferToImage(staging, commandPool, (uint)image.Width, (uint)image.Height);

                GenerateMipMaps(width, height, Format.R8G8B8A8Srgb, commandPool);
            }
            finally
            {
                staging.Dispose();
            }
        }

        ImageView = new(device, Image, Format.R8G8B8A8Srgb, mipLevels);
    }

    private void GenerateMipMaps(int width, int height, Format format, VkCommandPool commandPool)
    {
        device.Vk.GetPhysicalDeviceFormatProperties(device.PhysicalDevice, format, out var formatProperties);

        if ((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.SampledImageFilterLinearBit) == 0)
            throw new VkException("texture image format does not support linear blitting!");

        using VkCommandBuffer commandBuffer = new(device, commandPool);

        unsafe
        {
            var barrier = new ImageMemoryBarrier(
                image: Image!,
                srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
                dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
                subresourceRange: new(ImageAspectFlags.ColorBit, null, 1, 0, 1));

            for (uint i = 1u; i < MipLevels; i++)
            {
                barrier.SubresourceRange.BaseMipLevel = i - 1;
                barrier.OldLayout = ImageLayout.TransferDstOptimal;
                barrier.NewLayout = ImageLayout.TransferSrcOptimal;
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;

                commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, in barrier);

                //vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &barrier);

                var blit = new ImageBlit(new(ImageAspectFlags.ColorBit, i - 1, 0, 1), new(ImageAspectFlags.ColorBit, i, 0, 1))
                {
                    SrcOffsets = new() { Element0 = new(0, 0, 0), Element1 = new(width, height, 1) },
                    DstOffsets = new() { Element0 = new(0, 0, 0), Element1 = new(width > 1 ? width / 2 : 1, height > 1 ? height / 2 : 1, 1) },
                };

                commandBuffer.BlitImage(Image, ImageLayout.TransferSrcOptimal, Image, ImageLayout.TransferDstOptimal, blit, Filter.Linear);

                //vk.CmdBlitImage(commandBuffer, Image, ImageLayout.TransferSrcOptimal, Image, ImageLayout.TransferDstOptimal, 1, blit, Filter.Linear);

                barrier.OldLayout = ImageLayout.TransferSrcOptimal;
                barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
                barrier.SrcAccessMask = AccessFlags.TransferReadBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;

                commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, in barrier);

                //vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0, 0, null, 0, null, 1, barrier);

                if (width > 1)
                    width /= 2;

                if (height > 1)
                    height /= 2;
            }

            barrier.SubresourceRange.BaseMipLevel = MipLevels - 1;
            barrier.OldLayout = ImageLayout.TransferDstOptimal;
            barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, in barrier).End();
            commandBuffer.Submit();
        }
    }

    public void Dispose()
    {
        ImageView?.Dispose();
        Image?.Dispose();
    }
}*/