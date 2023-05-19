using System.Diagnostics;

using Silk.NET.Maths;

using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

using Silk.NET.Input;
using Silk.NET.Windowing;

using Vulkanoid;
using Vulkanoid.Vulkan;
using Vulkanoid.Sample;

var options = WindowOptions.DefaultVulkan;
options.Size = new(1280, 720);

var window = Window.Create(options);
window.Initialize();

var render = new VulkanRender(window);

window.Run();

class VulkanRender
{
    private readonly IWindow window;

    private readonly Stopwatch stopwatch = new();

    private readonly Vk vk = Vk.GetApi();

    VkDevice device;

    KhrSurface khrSurface;
    
    SurfaceKHR surface;

    VkRenderPass renderPass;

    VkCommandPool commandPool;

    VkSwapchain swapchain;

    VkPipeline pipeline;

    Shader vertexShader;
    Shader fragmentShader;

    VkDescriptorPool descriptorPool;

    VkDescriptorSetLayout descriptorSetLayout;

    //VkDescriptorSet descriptorSet;

    ModelViewProjection modelViewProjection;

    //VkTexture textureImage;
    VkSampler textureSampler;

    VkBuffer uniformBuffer;
    VkBuffer vertexBuffer;
    VkBuffer indexBuffer;

    VkFence imageFence;
    VkSemaphore imageAvailableSemaphore;
    VkSemaphore renderFinishedSemaphore;

    List<Vertex> vertices = new();
    List<uint> indices = new();

    public VulkanRender(IWindow window)
    {
        this.window = window;
        var input = window.CreateInput();
        var mouse = input.Mice[0];

        mouse.MouseMove += (s, e) =>
        {
            if (s.IsButtonPressed(MouseButton.Left))
            {
                int sign = e.X > 0 ? -1 : 1;

                modelViewProjection.Model *= Matrix4X4.CreateRotationZ(0.05f * sign);

                uniformBuffer.Load(modelViewProjection);
            }
        };

        mouse.Scroll += (s, e) =>
        {
            int sign = e.Y > 0 ? -1 : 1;

            //modelViewProjection.View = modelViewProjection.Model.(0.99f * sign);

            uniformBuffer.Load(modelViewProjection);
        };

        unsafe
        {
            uint extensionCount;
            vk.EnumerateInstanceExtensionProperties((byte*)null, &extensionCount, null);

            var extensionProperties = stackalloc ExtensionProperties[(int)extensionCount];
            vk.EnumerateInstanceExtensionProperties((byte*)null, &extensionCount, extensionProperties);

            byte** extensionNames = stackalloc byte*[(int)extensionCount];

            for (int i = 0; i < extensionCount; i++)
                extensionNames[i] = extensionProperties[i].ExtensionName;

            var appInfo = new ApplicationInfo(apiVersion: Vk.Version11);
            var createInfo = new InstanceCreateInfo(pApplicationInfo: &appInfo, ppEnabledExtensionNames: extensionNames, enabledExtensionCount: extensionCount);

            vk.CreateInstance(in createInfo, null, out var instanceHandle);

            //vk.CurrentInstance = instanceHandle;

            //surface = window.VkSurface!.Create<AllocationCallbacks>(instanceHandle.ToHandle(), null).ToSurface();

            device = GraphicsDevice.CreateVulkan(window.VkSurface!);
        }

        commandPool = device.CreateCommandPool();

        descriptorSetLayout = device.CreateDescriptorSetLayout();

        var extent = new Extent2D((uint)window.Size.X, (uint)window.Size.Y);

        var surfaceFormat = device.SwapchainSupport.Formats.FirstOrDefault(f => f.Format == Format.B8G8R8A8Srgb, device.SwapchainSupport.Formats[0]).Format;

        renderPass = device.CreateRenderPass(surfaceFormat, SampleCountFlags.Count8Bit);
        swapchain = device.CreateSwapchain(commandPool, renderPass, extent, surfaceFormat, SampleCountFlags.Count8Bit);

        var vertCode = Embedded.GetShaderBytes("shader.vert.spv");
        vertexShader = device.CreateShader(vertCode, "main", ShaderStageFlags.VertexBit);

        var fragCode = Embedded.GetShaderBytes("shader.frag.spv");
        fragmentShader = device.CreateShader(fragCode, "main", ShaderStageFlags.FragmentBit);

        pipeline = device.CreatePipeline(new Shader[] { vertexShader, fragmentShader }, descriptorSetLayout, renderPass, extent);

        unsafe
        {
            var fenceInfo = new FenceCreateInfo(flags: FenceCreateFlags.SignaledBit);
            imageFence = device.CreateFence(fenceInfo);

            var semaphoreInfo = new SemaphoreCreateInfo(flags: (uint)SemaphoreCreateFlags.None);
            imageAvailableSemaphore = device.CreateSemaphore(semaphoreInfo);
            renderFinishedSemaphore = device.CreateSemaphore(semaphoreInfo);
        }

        var model = SharpGLTF.Schema2.ModelRoot.Load("assets/grogu/scene.gltf");
        foreach (var mesh in model.LogicalMeshes)
        {
            foreach (var primitive in mesh.Primitives)
            {
                foreach (uint index in primitive.GetIndices())
                    indices.Add(index);

                var positions = primitive.VertexAccessors["POSITION"].AsVector3Array();
                var normals = primitive.VertexAccessors["NORMAL"].AsVector3Array();
                var texcoords = primitive.VertexAccessors["TEXCOORD_0"].AsVector2Array();

                for (int i = 0; i < positions.Count; i++)
                {
                    var position = positions[i];
                    var normal = normals[i];
                    var texcoord = texcoords[i];

                    vertices.Add(
                        new Vertex(
                            new Vector3D<float>(position.X, position.Y, position.Z),
                            new Vector3D<float>(normal.X, normal.Y, normal.Z),
                            new Vector2D<float>(texcoord.X, texcoord.Y)));
                }
            }
        }

        uniformBuffer = device.CreateBuffer<ModelViewProjection>(BufferUsageFlags.UniformBufferBit);
        uniformBuffer.Load(modelViewProjection);

        vertexBuffer = device.CreateBuffer<Vertex>(BufferUsageFlags.VertexBufferBit, vertices.Count);
        vertexBuffer.Load(vertices.ToArray());

        indexBuffer = device.CreateBuffer<uint>(BufferUsageFlags.IndexBufferBit, indices.Count);
        indexBuffer.Load(indices.ToArray());

        var modelImage = model.LogicalTextures[1].PrimaryImage.Content;

        //textureImage = device, commandPool, modelImage.Open());
        //textureSampler = new(device, textureImage.MipLevels);

        descriptorPool = device.CreateDescriptorPool();

        //descriptorSet = descriptorPool.CreateDescriptorSet(descriptorSetLayout, uniformBuffer, textureImage, textureSampler);

        modelViewProjection = new ModelViewProjection()
        {
            Model = Matrix4X4<float>.Identity,
            View = Matrix4X4.CreateLookAt(new Vector3D<float>(5f, 0f, 0f), Vector3D<float>.Zero, Vector3D<float>.UnitZ),
            Projection = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45f), swapchain.Extent.Width / (float)swapchain.Extent.Height, 0.001f, 10000f)
        };

        device.WaitIdle();

        window.FramebufferResize += OnFramebufferResize;
        window.Render += DrawFrame;

        stopwatch.Start();
    }

    void OnFramebufferResize(Vector2D<int> obj) => RecreateSwapchain();

    void RecreateSwapchain()
    {
        device.WaitIdle();

        swapchain.Dispose();
        pipeline.Dispose();

        var extent = new Extent2D((uint)window.Size.X, (uint)window.Size.Y);

        var surfaceFormat = device.SwapchainSupport.Formats.FirstOrDefault(f => f.Format == Format.B8G8R8A8Srgb, device.SwapchainSupport.Formats[0]).Format;

        swapchain = device.CreateSwapchain(commandPool, renderPass, extent, surfaceFormat, SampleCountFlags.Count8Bit);

        pipeline = device.CreatePipeline(new Shader[] { vertexShader, fragmentShader }, descriptorSetLayout, renderPass, extent);
    }

    void Dispose()
    {
        window.Render -= DrawFrame;

        descriptorSetLayout.Dispose();

        uniformBuffer.Dispose();

        //textureImage.Dispose();
        textureSampler.Dispose();

        //descriptorSet.Dispose();

        swapchain.Dispose();
        indexBuffer.Dispose();
        vertexBuffer.Dispose();
        commandPool.Dispose();
    }

    void DrawFrame(double obj)
    {
        imageFence.Wait(ulong.MaxValue);
        imageFence.Reset();

        unsafe
        {
            var result = swapchain.AcquireNextImage(imageAvailableSemaphore, out uint imageIndex);

            if (result == Result.ErrorOutOfDateKhr)
            {
                RecreateSwapchain();
                return;
            }

            if (result is not Result.Success and not Result.SuboptimalKhr)
                throw new Exception("failed to acquire swapchain image");

            using var commandBuffer = commandPool.CreateCommandBuffer();

            unsafe
            {
                var clearValues = stackalloc ClearValue[2]
                {
                    new(color: new() { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 }),
                    new(depthStencil: new(1, 0))
                };

                var renderPassInfo = new RenderPassBeginInfo(
                    renderPass: renderPass,
                    framebuffer: swapchain.Framebuffers[imageIndex],
                    renderArea: new Rect2D { Offset = new() { X = 0, Y = 0 }, Extent = swapchain.Extent },
                    clearValueCount: 2,
                    pClearValues: clearValues);

                commandBuffer.BeginRenderPass(renderPassInfo)
                             .BindPipeline(pipeline)
                             .BindVertexBuffer(vertexBuffer)
                             .BindIndexBuffer(indexBuffer)
                             //.BindDescriptorSet(descriptorSet, pipeline.Layout)
                             //.PushConstant(modelViewProjection.Model, pipeline.Layout, ShaderStageFlags.VertexBit)
                             .DrawIndexed((uint)indices.Count)
                             //.Draw((uint)vertices.Count)
                             .EndRenderPass();
            }

            var waitStage = PipelineStageFlags.ColorAttachmentOutputBit;

            var imageAvailableSemaphoreHandle = (Semaphore)imageAvailableSemaphore;
            var renderFinishedSemaphoreHandle = (Semaphore)renderFinishedSemaphore;

            var submitInfo = new SubmitInfo(
                pWaitSemaphores: &imageAvailableSemaphoreHandle,
                waitSemaphoreCount: 1u,
                pSignalSemaphores: &renderFinishedSemaphoreHandle,
                signalSemaphoreCount: 1u,
                pWaitDstStageMask: &waitStage);

            commandBuffer.Submit(submitInfo, imageFence);

            result = swapchain.Present(renderFinishedSemaphore, imageIndex);

            if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr)
                RecreateSwapchain();
            else if (result != Result.Success)
                throw new Exception("failed to present swapchain image");
        }
    }
}