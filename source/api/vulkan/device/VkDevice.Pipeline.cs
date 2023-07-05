namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    internal void DestroyPipeline(Pipeline pipeline)
    {
        unsafe
        {
            vk.DestroyPipeline(handle, pipeline, null);
        }
    }

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

                vk.CreatePipelineLayout(handle, pipelineLayoutInfo, null, out var layoutHandle).Check();

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
}
