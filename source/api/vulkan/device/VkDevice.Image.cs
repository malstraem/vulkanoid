using SixLabors.ImageSharp.PixelFormats;
using RawImage = SixLabors.ImageSharp.Image;

namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    #region Image
    [Obsolete("To do - add flexibility")]
    public VkImage CreateImage(Extent3D extent, Format format, ImageTiling imageTiling, ImageUsageFlags imageUsage,
                               MemoryPropertyFlags properties, SampleCountFlags sampleCount = SampleCountFlags.Count1Bit, uint mipLevels = 1)
    {
        unsafe
        {
            var imageInfo = new ImageCreateInfo(
                imageType: ImageType.Type2D,
                extent: extent,
                mipLevels: mipLevels,
                arrayLayers: 1,
                format: format,
                tiling: imageTiling,
                initialLayout: ImageLayout.Undefined,
                usage: imageUsage,
                sharingMode: SharingMode.Exclusive,
                samples: sampleCount);

            vk.CreateImage(handle, in imageInfo, null, out var imageHandle).Check();
            vk.GetImageMemoryRequirements(handle, imageHandle, out var memoryRequirements);

            return new VkImage(imageHandle, AllocateMemory(memoryRequirements, properties), this) { Format = format, MipLevels = mipLevels };
        }
    }

    public VkImage CreateImage(Stream stream, VkCommandPool commandPool)
    {
        using var rawImage = RawImage.Load<Rgba32>(stream);
        uint mipLevels = (uint)(Math.Floor(Math.Log2(Math.Max(rawImage.Width, rawImage.Height))) + 1);
        int width = rawImage.Width;
        int height = rawImage.Height;
        int imageSize = width * height * 4;

        Span<Rgba32> pixelData = new(new Rgba32[width * height]);
        rawImage.CopyPixelDataTo(pixelData);

        var stagingBuffer = CreateBuffer<Rgba32>(BufferUsageFlags.TransferSrcBit, elementCount: pixelData.Length);
        stagingBuffer.Upload(pixelData);

        var image = CreateImage(
            new Extent3D((uint)width, (uint)height, 1u), Format.R8G8B8A8Srgb, ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit, mipLevels: mipLevels);

        image.TransitionImageLayout(commandPool, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, mipLevels);

        stagingBuffer.CopyToImage(image, commandPool, (uint)width, (uint)height);

        image.TransitionImageLayout(commandPool, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, mipLevels);
        image.GenerateMipMaps(width, height, mipLevels, Format.R8G8B8A8Srgb, commandPool);

        return image;
    }
    #endregion

    #region ImageView
    [Obsolete("To do - add flexibility")]
    public VkImageView CreateImageView(Image image, Format format, uint mipLevels, ImageAspectFlags aspectFlags = ImageAspectFlags.ColorBit)
    {
        unsafe
        {
            var createInfo = new ImageViewCreateInfo(
                image: image,
                viewType: ImageViewType.Type2D,
                format: format,
                components: new ComponentMapping(ComponentSwizzle.Identity, ComponentSwizzle.Identity, ComponentSwizzle.Identity, ComponentSwizzle.Identity),
                subresourceRange: new ImageSubresourceRange
                {
                    AspectMask = aspectFlags,
                    BaseMipLevel = 0,
                    LevelCount = mipLevels,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                });

            vk.CreateImageView(handle, createInfo, null, out var imageViewHandle).Check();

            return new VkImageView(imageViewHandle, this);
        }
    }

    public VkImageView CreateImageView(VkImage image, ImageAspectFlags aspectFlags = ImageAspectFlags.ColorBit) 
        => CreateImageView(image, image.Format, image.MipLevels, aspectFlags);
    #endregion

    #region Sampler & Framebuffer
    [Obsolete("To do - add flexibility")]
    public VkSampler CreateSampler(uint maxLod)
    {
        unsafe
        {
            vk.GetPhysicalDeviceProperties(physicalDevice, out var deviceProperties);

            var samplerInfo = new SamplerCreateInfo(
                magFilter: Filter.Linear,
                minFilter: Filter.Linear,
                addressModeU: SamplerAddressMode.Repeat,
                addressModeV: SamplerAddressMode.Repeat,
                addressModeW: SamplerAddressMode.Repeat,
                anisotropyEnable: true, // if device support
                maxAnisotropy: deviceProperties.Limits.MaxSamplerAnisotropy, // if device support else 1
                borderColor: BorderColor.IntOpaqueBlack,
                unnormalizedCoordinates: false,
                compareEnable: false,
                compareOp: CompareOp.Always,
                mipmapMode: SamplerMipmapMode.Linear,
                mipLodBias: 0u,
                minLod: 0u,
                maxLod: maxLod);

            vk.CreateSampler(handle, in samplerInfo, null, out var samplerHandle).Check();

            return new VkSampler(samplerHandle, this);
        }
    }

    [Obsolete("To do - add flexibility")]
    public VkFramebuffer CreateFramebuffer(VkImageView imageView, VkImageView depthImageView, VkImageView colorImageView,
                                           VkRenderPass renderPass, Extent2D extent)
    {
        unsafe
        {
            var attachments = stackalloc ImageView[3];

            attachments[0] = colorImageView;
            attachments[1] = depthImageView;
            attachments[2] = imageView;

            var framebufferInfo = new FramebufferCreateInfo(
                renderPass: renderPass,
                attachmentCount: 3u,
                pAttachments: attachments,
                width: extent.Width,
                height: extent.Height,
                layers: 1u);

            vk.CreateFramebuffer(handle, framebufferInfo, null, out var framebufferHandle).Check();

            return new VkFramebuffer(framebufferHandle, this);
        }
    }
    #endregion
}
