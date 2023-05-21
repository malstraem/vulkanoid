using System.Runtime.CompilerServices;

using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan.Extensions.KHR;
using Buffer = Silk.NET.Vulkan.Buffer;

using SixLabors.ImageSharp.PixelFormats;
using RawImage = SixLabors.ImageSharp.Image;

namespace Vulkanoid.Vulkan;

public sealed class VkDevice : GraphicsDevice
{
    private readonly Device handle;

    private readonly SurfaceKHR surfaceHandle;

    internal readonly VkInstance instance;

    internal readonly VkPhysicalDevice physicalDevice;

    internal Vk vk = Vk.GetApi();

    private VkQueue GetQueue(uint queueFamily) => new(vk.GetDeviceQueue(handle, queueFamily, 0), this);

    public VkDevice(IVkSurface surface)
    {
        unsafe
        {
            byte** extensions = surface.GetRequiredExtensions(out uint count);

            string[] extensionNames = SilkMarshal.PtrToStringArray((nint)extensions, (int)count);

            instance = new VkInstance(vk, extensionNames.ToList());
        }

        physicalDevice = instance.GetPhysicalDevices(surface).First(); // todo: allow to list and pick devices

        handle = physicalDevice.CreateDevice();

        GraphicsQueue = GetQueue(physicalDevice.queueFamilies.GraphicsIndex);

        unsafe
        {
            surfaceHandle = surface.Create<AllocationCallbacks>(instance.handle.ToHandle(), null).ToSurface();
        }
    }

    ~VkDevice()
    {
        unsafe
        {
            vk.DestroyDevice(handle, null);
        }
    }

    public static implicit operator Device(VkDevice resource) => resource.handle;

    #region Synchronization
    public VkFence CreateFence(in FenceCreateInfo info)
    {
        unsafe
        {
            vk.CreateFence(handle, info, null, out var fenceHandle);
            return new VkFence(fenceHandle, this);
        }
    }

    public VkSemaphore CreateSemaphore(in SemaphoreCreateInfo info)
    {
        unsafe
        {
            vk.CreateSemaphore(handle, info, null, out var semaphore);
            return new VkSemaphore(semaphore, this);
        }
    }

    public void WaitIdle() => vk.DeviceWaitIdle(handle);

    internal void WaitForFence(in Fence fence, ulong timeout)
    {
        var result = vk.WaitForFences(handle, 1u, fence, true, timeout);
    }

    internal void ResetFence(in Fence fence)
    {
        var result = vk.ResetFences(handle, 1u, fence);
    }
    #endregion Synchronization

    #region Allocating
    internal CommandBuffer AllocateCommandBuffer(in CommandBufferAllocateInfo info)
    {
        vk.AllocateCommandBuffers(handle, in info, out var commandBufferHandle);
        return commandBufferHandle;
    }

    internal VkDescriptorSet AllocateDescriptorSet(in DescriptorSetAllocateInfo allocateInfo)
    {
        vk.AllocateDescriptorSets(handle, in allocateInfo, out var setHandle);

        return new VkDescriptorSet(setHandle, this);
    }
    #endregion

    #region CommandPool
    public VkCommandPool CreateCommandPool()
    {
        unsafe
        {
            var info = new CommandPoolCreateInfo(queueFamilyIndex: physicalDevice.queueFamilies.GraphicsIndex);

            vk.CreateCommandPool(handle, info, null, out var poolHandle);

            return new VkCommandPool(poolHandle, this);
        }
    }

    public void DestroyCommandPool(CommandPool poolHandle)
    {
        unsafe
        {
            vk.DestroyCommandPool(handle, poolHandle, null);
        }
    }
    #endregion

    #region CommandBuffer
    public void FreeCommandBuffer(VkCommandBuffer commandBuffer) => vk.FreeCommandBuffers(handle, commandBuffer.commandPool, 1u, commandBuffer);
    #endregion

    #region Buffer
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

            vk.CreateBuffer(handle, in info, null, out var bufferHandle);
            vk.GetBufferMemoryRequirements(handle, bufferHandle, out var memoryRequirements);

            var memory = AllocateMemory(memoryRequirements, properties);

            return new VkBuffer(bufferHandle, memory, this);
        }
    }

    public void DestroyBuffer(Buffer bufferHandle)
    {
        unsafe
        {
            vk.DestroyBuffer(handle, bufferHandle, null);
        }
    }
    #endregion

    #region Memory
    internal VkMemory AllocateMemory(MemoryRequirements requirements, MemoryPropertyFlags properties)
    {
        unsafe
        {
            var allocateInfo = new MemoryAllocateInfo(allocationSize: requirements.Size,
                memoryTypeIndex: physicalDevice.FindMemoryType(requirements.MemoryTypeBits, properties));

            vk.AllocateMemory(handle, in allocateInfo, null, out var memoryHandle);

            return new VkMemory(memoryHandle, this) { Size = requirements.Size };
        }
    }

    internal nint MapMemory(DeviceMemory memory, ulong size)
    {
        unsafe
        {
            void* data;
            var result = vk.MapMemory(handle, memory, 0u, size, 0u, &data);

            return (nint)data;
        }
    }

    internal void UnmapMemory(DeviceMemory memory) => vk.UnmapMemory(handle, memory);

    internal void BindBufferMemory(Buffer bufferHandle, DeviceMemory memoryHandle)
    {
        var result = vk.BindBufferMemory(handle, bufferHandle, memoryHandle, 0);
    }

    internal void BindImageMemory(Image imageHandle, DeviceMemory memoryHandle) => vk.BindImageMemory(handle, imageHandle, memoryHandle, 0u);

    internal void FreeMemory(DeviceMemory memoryHandle)
    {
        unsafe
        {
            vk.FreeMemory(handle, memoryHandle, null);
        }
    }
    #endregion

    #region Descriptoring
    [Obsolete("To do - add flexibility")]
    public VkDescriptorPool CreateDescriptorPool()
    {
        unsafe
        {
            var poolSizesPtr = stackalloc DescriptorPoolSize[2]
            {
                new(type: DescriptorType.UniformBuffer, 1u),
                new(type: DescriptorType.CombinedImageSampler, 1u)
            };

            var poolInfo = new DescriptorPoolCreateInfo(poolSizeCount: 2, pPoolSizes: poolSizesPtr, maxSets: 1u);

            vk.CreateDescriptorPool(handle, poolInfo, null, out var poolHandle);

            return new VkDescriptorPool(poolHandle, this);
        }
    }

    [Obsolete("To do - add flexibility")]
    public VkDescriptorSetLayout CreateDescriptorSetLayout()
    {
        unsafe
        {
            var layoutBinding = new DescriptorSetLayoutBinding(0u, DescriptorType.UniformBuffer, descriptorCount: 1u, stageFlags: ShaderStageFlags.VertexBit);

            var samplerLayoutBinding = new DescriptorSetLayoutBinding(1u, DescriptorType.CombinedImageSampler, 1u, ShaderStageFlags.FragmentBit);

            var bindingsPtr = stackalloc DescriptorSetLayoutBinding[2] 
            { 
                layoutBinding, 
                samplerLayoutBinding 
            };

            var createInfo = new DescriptorSetLayoutCreateInfo(bindingCount: 2u, pBindings: bindingsPtr);

            vk.CreateDescriptorSetLayout(handle, in createInfo, null, out var setLayoutHandle);
            
            return new VkDescriptorSetLayout(setLayoutHandle, this);
        }
    }

    public void UpdateDescriptorSet(ReadOnlySpan<WriteDescriptorSet> writeDescriptorSets) 
        => vk.UpdateDescriptorSets(handle, writeDescriptorSets, default);
    #endregion

    #region RenderPass
    public VkRenderPass CreateRenderPass(Format format, SampleCountFlags sampleCount)
    {
        var colorAttachment = new AttachmentDescription
        {
            Format = format,
            Samples = sampleCount,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.ColorAttachmentOptimal
        };

        var colorAttachmentResolve = new AttachmentDescription
        {
            Format = format,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.DontCare,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };

        var depthAttachment = new AttachmentDescription
        {
            Format = physicalDevice.DepthFormat,
            Samples = sampleCount,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
        };

        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        var depthAttachmentRef = new AttachmentReference
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal
        };

        var colorAttachmentResolveRef = new AttachmentReference
        {
            Attachment = 2,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        unsafe
        {
            var subpass = new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
                PDepthStencilAttachment = &depthAttachmentRef,
                PResolveAttachments = &colorAttachmentResolveRef
            };

            var dependency = new SubpassDependency
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                DstAccessMask = AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
            };

            var attachments = stackalloc AttachmentDescription[3] { colorAttachment, depthAttachment, colorAttachmentResolve };

            var renderPassInfo = new RenderPassCreateInfo(
                attachmentCount: 3,
                pAttachments: attachments,
                subpassCount: 1,
                pSubpasses: &subpass,
                dependencyCount: 1,
                pDependencies: &dependency);

            vk.CreateRenderPass(handle, renderPassInfo, null, out var renderPassHandle);

            return new VkRenderPass(renderPassHandle, this);
        }
    }
    #endregion

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

            vk.CreateImage(handle, in imageInfo, null, out var imageHandle);

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

        var stagingBuffer = CreateBuffer<Rgba32>(BufferUsageFlags.BufferUsageTransferSrcBit, pixelData.Length);
        stagingBuffer.Upload(pixelData);

        var image = CreateImage(new Extent3D((uint)width, (uint)height, 1u), Format.R8G8B8A8Srgb, ImageTiling.Optimal,
            ImageUsageFlags.ImageUsageTransferDstBit | ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageSampledBit,
            MemoryPropertyFlags.MemoryPropertyDeviceLocalBit, mipLevels: mipLevels);

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
                components: new ComponentMapping(ComponentSwizzle.Identity, 
                                                ComponentSwizzle.Identity, 
                                                ComponentSwizzle.Identity, 
                                                ComponentSwizzle.Identity),
                subresourceRange: new ImageSubresourceRange
                {
                    AspectMask = aspectFlags,
                    BaseMipLevel = 0,
                    LevelCount = mipLevels,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                });

            vk.CreateImageView(handle, createInfo, null, out var imageViewHandle);

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

            vk.CreateSampler(handle, in samplerInfo, null, out var samplerHandle);

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

            var result = vk.CreateFramebuffer(handle, framebufferInfo, null, out var framebufferHandle);

            return new VkFramebuffer(framebufferHandle, this);
        }
    }
    #endregion

    #region Pipeline
    [Obsolete("To do - add flexibility")]
    public VkPipeline CreatePipeline(Shader[] shaders, VkDescriptorSetLayout descriptorSetLayout, VkRenderPass renderPass, Extent2D extent)
    {
        unsafe
        {
            var shaderStages = stackalloc PipelineShaderStageCreateInfo[shaders.Length];

            for (int i = 0; i < shaders.Length; i++)
            {
                var shaderStageInfo = new PipelineShaderStageCreateInfo(
                    stage: shaders[i].Stage,
                    module: shaders[i].Module,
                    pName: (byte*)SilkMarshal.StringToPtr(shaders[i].EntryPoint));

                shaderStages[i] = shaderStageInfo;
            }

            var bindingDescription = Vertex.BindingDescription;
            var attributeDescriptions = Vertex.AttributeDescriptions;

            fixed (VertexInputAttributeDescription* pAttributeDescriptions = attributeDescriptions)
            {
                var vertexInputInfo = new PipelineVertexInputStateCreateInfo(
                    vertexBindingDescriptionCount: 1u,
                    vertexAttributeDescriptionCount: (uint)attributeDescriptions.Length,
                    pVertexBindingDescriptions: &bindingDescription,
                    pVertexAttributeDescriptions: pAttributeDescriptions);

                var inputAssembly = new PipelineInputAssemblyStateCreateInfo(
                    topology: PrimitiveTopology.TriangleList,
                    primitiveRestartEnable: false);

                var viewport = new Viewport()
                {
                    X = 0f,
                    Y = 0f,
                    Width = extent.Width,
                    Height = extent.Height,
                    MinDepth = 0f,
                    MaxDepth = 1f
                };

                var scissor = new Rect2D() { Offset = default, Extent = extent };

                var viewportState = new PipelineViewportStateCreateInfo(
                    viewportCount: 1u,
                    pViewports: &viewport,
                    scissorCount: 1u,
                    pScissors: &scissor);

                var rasterizer = new PipelineRasterizationStateCreateInfo(
                    depthClampEnable: false,
                    rasterizerDiscardEnable: false,
                    polygonMode: PolygonMode.Fill,
                    lineWidth: 1u,
                    cullMode: CullModeFlags.BackBit,
                    frontFace: FrontFace.Clockwise,
                    depthBiasEnable: false);

                var multisampling = new PipelineMultisampleStateCreateInfo(
                    sampleShadingEnable: false,
                    rasterizationSamples: SampleCountFlags.Count8Bit);

                var colorBlendAttachment = new PipelineColorBlendAttachmentState()
                {
                    ColorWriteMask = ColorComponentFlags.RBit |
                                     ColorComponentFlags.GBit |
                                     ColorComponentFlags.BBit |
                                     ColorComponentFlags.ABit,
                    BlendEnable = false
                };

                var colorBlending = new PipelineColorBlendStateCreateInfo(
                    logicOpEnable: false,
                    logicOp: LogicOp.Copy,
                    attachmentCount: 1u,
                    pAttachments: &colorBlendAttachment);

                colorBlending.BlendConstants[0] = 0;
                colorBlending.BlendConstants[1] = 0;
                colorBlending.BlendConstants[2] = 0;
                colorBlending.BlendConstants[3] = 0;

                DescriptorSetLayout descriptorSetLayoutHandle = descriptorSetLayout;

                var pipelineLayoutInfo = new PipelineLayoutCreateInfo(
                    setLayoutCount: 1u,
                    pSetLayouts: &descriptorSetLayoutHandle);

                vk.CreatePipelineLayout(handle, pipelineLayoutInfo, null, out var layoutHandle);

                var depthStencil = new PipelineDepthStencilStateCreateInfo(
                    depthTestEnable: true,
                    depthWriteEnable: true,
                    depthCompareOp: CompareOp.Less,
                    depthBoundsTestEnable: false,
                    minDepthBounds: 0u,
                    maxDepthBounds: 1u,
                    stencilTestEnable: false);

                var pipelineInfo = new GraphicsPipelineCreateInfo(
                    stageCount: (uint)shaders.Length,
                    pStages: shaderStages,
                    pVertexInputState: &vertexInputInfo,
                    pInputAssemblyState: &inputAssembly,
                    pViewportState: &viewportState,
                    pRasterizationState: &rasterizer,
                    pMultisampleState: &multisampling,
                    pColorBlendState: &colorBlending,
                    layout: layoutHandle,
                    renderPass: renderPass,
                    subpass: 0u,
                    basePipelineHandle: default,
                    pDepthStencilState: &depthStencil);

                var result = vk.CreateGraphicsPipelines(handle, default, 1u, pipelineInfo, null, out var pipelineHandle);

                return new VkPipeline(pipelineHandle, layoutHandle, this);
            }
        }
    }
    #endregion

    #region Shader
    public Shader CreateShader(byte[] code, string entryPoint, ShaderStageFlags shaderStage)
    {
        unsafe
        {
            var createInfo = new ShaderModuleCreateInfo(codeSize: (nuint)code.Length);

            fixed (byte* codePtr = code)
                createInfo.PCode = (uint*)codePtr;

            vk.CreateShaderModule(handle, &createInfo, null, out var moduleHandle);

            var shaderModule = new VkShaderModule(moduleHandle, this);

            return new Shader { EntryPoint = entryPoint, Module = shaderModule, Stage = shaderStage };
        }
    }

    public void DestroyShaderModule(VkShaderModule module)
    {
        unsafe
        {
            vk.DestroyShaderModule(handle, module, null);
        }
    }
    #endregion

    #region Swapchain
    [Obsolete("To do - add flexibility")]
    public VkSwapchain CreateSwapchain(VkCommandPool commandPool, VkRenderPass renderPass, Extent2D extent,
                                       Format format, SampleCountFlags sampleCount)
    {
        var swapchainSupport = physicalDevice.swapchainSupport;

        var surfaceFormat = swapchainSupport.Formats.FirstOrDefault(f => f.Format == Format.B8G8R8A8Srgb, swapchainSupport.Formats[0]);
        format = surfaceFormat.Format;

        var presentMode = swapchainSupport.PresentModes.FirstOrDefault(p => p == PresentModeKHR.MailboxKhr, PresentModeKHR.FifoKhr);

        if (swapchainSupport.Capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            extent = swapchainSupport.Capabilities.CurrentExtent;
        }
        else
        {
            extent.Width = Math.Max(swapchainSupport.Capabilities.MinImageExtent.Width, Math.Min(swapchainSupport.Capabilities.MaxImageExtent.Width, extent.Width));
            extent.Height = Math.Max(swapchainSupport.Capabilities.MinImageExtent.Height, Math.Min(swapchainSupport.Capabilities.MaxImageExtent.Height, extent.Height));
        }

        uint imageCount = swapchainSupport.Capabilities.MinImageCount + 1;

        if (swapchainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapchainSupport.Capabilities.MaxImageCount)
            imageCount = swapchainSupport.Capabilities.MaxImageCount;

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

            var families = physicalDevice.queueFamilies;

            if (families.GraphicsIndex != families.PresentIndex)
            {
                uint* indices = stackalloc [] { families.GraphicsIndex, families.PresentIndex!.Value };

                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndexCount = 2;
                createInfo.PQueueFamilyIndices = indices;
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
            }

            if (!vk.TryGetDeviceExtension(instance, handle, out KhrSwapchain swapchainExtension))
                throw new Exception("Presenting requested but not supported");

            var result = swapchainExtension.CreateSwapchain(handle, createInfo, null, out var swapchainHandle);

            var depthImage = CreateImage(new Extent3D(extent.Width, extent.Height, 1u), physicalDevice.DepthFormat, ImageTiling.Optimal,
                                ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, sampleCount);

            //depthImage.TransitionImageLayout(commandPool, format, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal, 1u);

            var depthView = CreateImageView(depthImage, ImageAspectFlags.DepthBit);

            var sampleImage = CreateImage(new Extent3D(extent.Width, extent.Height, 1u), format, ImageTiling.Optimal,
                                            ImageUsageFlags.TransientAttachmentBit | ImageUsageFlags.ColorAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, sampleCount);

            var sampleView = CreateImageView(sampleImage, ImageAspectFlags.ColorBit);

            swapchainExtension.GetSwapchainImages(handle, swapchainHandle, ref imageCount, null);

            var images = new Span<Image>(new Image[imageCount]);
            swapchainExtension.GetSwapchainImages(handle, swapchainHandle, &imageCount, images);

            var framebuffers = new VkFramebuffer[images.Length];

            for (int i = 0; i < images.Length; i++)
            {
                var imageView = CreateImageView(images[i], format, 1u);

                framebuffers[i] = CreateFramebuffer(imageView, depthView, sampleView, renderPass, extent);
            }

            return new VkSwapchain(swapchainHandle, extent, framebuffers, GetQueue(physicalDevice.queueFamilies.PresentIndex.Value), swapchainExtension, this);
        }
    }
    #endregion

    public SwapchainSupport SwapchainSupport => physicalDevice.swapchainSupport;

    public VkQueue GraphicsQueue { get; }
}