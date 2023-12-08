using Silk.NET.Vulkan.Extensions.KHR;

namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    [Obsolete("add flexibility")]
    private VkSwapchain CreateSwapchain(in SwapchainCreateInfoKHR createInfo, SampleCountFlags sampleCount, uint presentFamily, VkRenderPass renderPass)
    {
        Image[] images;
        Extent3D extent = new(createInfo.ImageExtent.Width, createInfo.ImageExtent.Height, 1u);
        SwapchainKHR swapchainHandle;

        unsafe
        {
            swapchainExt.CreateSwapchain(handle, createInfo, null, out swapchainHandle).Check();

            uint imageCount = createInfo.MinImageCount;
            swapchainExt.GetSwapchainImages(handle, swapchainHandle, ref imageCount, null).Check();

            images = new Image[createInfo.MinImageCount];
            swapchainExt.GetSwapchainImages(handle, swapchainHandle, &imageCount, images).Check();
        }

        var depthImage = CreateImage(extent, physicalDevice.DepthFormat, ImageTiling.Optimal,
                                     ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, sampleCount);

        //depthImage.TransitionImageLayout(commandPool, format, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal, 1u);

        var depthView = CreateImageView(depthImage, ImageAspectFlags.DepthBit);

        var sampleImage = CreateImage(new Extent3D(extent.Width, extent.Height, 1u), createInfo.ImageFormat, ImageTiling.Optimal,
                                      ImageUsageFlags.TransientAttachmentBit | ImageUsageFlags.ColorAttachmentBit,
                                      MemoryPropertyFlags.DeviceLocalBit, sampleCount);

        var sampleView = CreateImageView(sampleImage, ImageAspectFlags.ColorBit);

        var framebuffers = new VkFramebuffer[images.Length];

        for (int i = 0; i < images.Length; i++)
        {
            var imageView = CreateImageView(images[i], createInfo.ImageFormat, 1u);
            framebuffers[i] = CreateFramebuffer(imageView, depthView, sampleView, renderPass, createInfo.ImageExtent);
        }

        return new VkSwapchain(swapchainHandle, this, GetQueue(presentFamily), swapchainExt)
        {
            Extent = createInfo.ImageExtent,
            Framebuffers = framebuffers,
        };
    }

    internal void DestroySwapchain(SwapchainKHR swapchainHandle)
    {
        unsafe
        {
            swapchainExt!.DestroySwapchain(handle, swapchainHandle, null);
        }
    }

    [Obsolete("add flexibility")]
    public VkSwapchain CreateSwapchain(SurfaceKHR surfaceHandle, Extent2D extent, Format format, SampleCountFlags sampleCount, VkRenderPass renderPass)
    {
        if (swapchainExt is null)
            throw new Exception($"Presentation requested but {KhrSwapchain.ExtensionName} is not supported");

        if (instance.surfaceExt is null)
            throw new Exception($"Presentation requsted but {KhrSurface.ExtensionName} is not supported");

        var swapchainSupport = GetSwapchainSupport(surfaceHandle);
        var capabilities = swapchainSupport.Capabilities;

        var surfaceFormat = swapchainSupport.Formats.FirstOrDefault(f => f.Format == Format.B8G8R8A8Srgb, swapchainSupport.Formats[0]);
        var presentMode = swapchainSupport.PresentModes.FirstOrDefault(p => p == PresentModeKHR.MailboxKhr, PresentModeKHR.FifoKhr);

        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            extent = capabilities.CurrentExtent;
        }
        else
        {
            extent.Width = Math.Max(capabilities.MinImageExtent.Width, Math.Min(capabilities.MaxImageExtent.Width, extent.Width));
            extent.Height = Math.Max(capabilities.MinImageExtent.Height, Math.Min(capabilities.MaxImageExtent.Height, extent.Height));
        }

        uint imageCount = capabilities.MinImageCount + 1;

        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
            imageCount = capabilities.MaxImageCount;

        uint presentFamily = physicalDevice.queueFamilies.Graphics;

        for (uint i = 0u; i < physicalDevice.queueFamilies.Count; i++)
        {
            instance.surfaceExt.GetPhysicalDeviceSurfaceSupport(physicalDevice, i, surfaceHandle, out var presentSupported).Check();

            if (presentSupported && i != physicalDevice.queueFamilies.Graphics)
            {
                presentFamily = i;
                break;
            }
        }

        unsafe
        {
            var createInfo = new SwapchainCreateInfoKHR(
                surface: surfaceHandle,
                minImageCount: imageCount,
                imageFormat: surfaceFormat.Format,
                imageColorSpace: surfaceFormat.ColorSpace,
                imageExtent: extent,
                imageArrayLayers: 1u,
                imageUsage: ImageUsageFlags.ColorAttachmentBit,
                preTransform: swapchainSupport.Capabilities.CurrentTransform,
                compositeAlpha: CompositeAlphaFlagsKHR.OpaqueBitKhr,
                presentMode: presentMode,
                clipped: true,
                oldSwapchain: default);

            if (graphicsQueue.family != presentFamily)
            {
                uint* indices = stackalloc [] { graphicsQueue.family, presentFamily };

                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndexCount = 2;
                createInfo.PQueueFamilyIndices = indices;
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
            }

            return CreateSwapchain(in createInfo, sampleCount, presentFamily, renderPass);
        }
    }

    public SwapchainSupport GetSwapchainSupport(SurfaceKHR surface)
    {
        var surfaceExt = instance.surfaceExt;

        uint formatCount = 0u, presentModeCount = 0u;
        surfaceExt.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out var surfaceCapabilities).Check();

        unsafe
        {
            surfaceExt.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, null).Check();

            Span<SurfaceFormatKHR> formats = stackalloc SurfaceFormatKHR[(int)formatCount];
            surfaceExt.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, formats).Check();

            surfaceExt.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &presentModeCount, null).Check();

            Span<PresentModeKHR> presentModes = stackalloc PresentModeKHR[(int)presentModeCount];
            surfaceExt.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &presentModeCount, presentModes).Check();

            return new(surfaceCapabilities, formats.ToArray(), presentModes.ToArray());
        }
    }
}
