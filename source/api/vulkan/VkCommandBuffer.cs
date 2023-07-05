using Buffer = Silk.NET.Vulkan.Buffer;

namespace Vulkanoid.Vulkan;

[Handle<CommandBuffer>]
public sealed partial class VkCommandBuffer : IDisposable
{
    internal readonly VkCommandPool commandPool;

    public VkCommandBuffer(CommandBuffer handle, VkDevice device, VkCommandPool commandPool) : this(handle, device) => this.commandPool = commandPool;

    public void OneTimeSubmit(Action<VkCommandBuffer> action)
    {
        action(this);

        unsafe
        {
            Submit(new SubmitInfo(sType: StructureType.SubmitInfo));
        }
    }

    public void Submit(SubmitInfo? info = null, Fence? fence = null)
    {
        var result = device.vk.EndCommandBuffer(handle);

        unsafe
        {
            var submitInfo = info ?? new SubmitInfo(sType: StructureType.SubmitInfo);

            submitInfo.CommandBufferCount = 1;

            fixed (CommandBuffer* handlePtr = &handle)
                submitInfo.PCommandBuffers = handlePtr;

            device.graphicsQueue.Submit(submitInfo);
        }

        device.graphicsQueue.WaitIdle();
    }

    public VkCommandBuffer BeginRenderPass(RenderPassBeginInfo info)
    {
        device.vk.CmdBeginRenderPass(handle, info, SubpassContents.Inline);
        return this;
    }

    public VkCommandBuffer EndRenderPass()
    {
        device.vk.CmdEndRenderPass(handle);
        return this;
    }

    public VkCommandBuffer BindPipeline(Pipeline pipeline)
    {
        device.vk.CmdBindPipeline(handle, PipelineBindPoint.Graphics, pipeline);
        return this;
    }

    public VkCommandBuffer BindVertexBuffer(Buffer vertexBuffer)
    {
        device.vk.CmdBindVertexBuffers(handle, 0u, 1u, vertexBuffer, 0ul);
        return this;
    }

    public VkCommandBuffer BindIndexBuffer(Buffer indexBuffer)
    {
        device.vk.CmdBindIndexBuffer(handle, indexBuffer, 0u, IndexType.Uint32);
        return this;
    }

    public VkCommandBuffer BindDescriptorSet(DescriptorSet descriptorSet, PipelineLayout pipelineLayout)
    {
        unsafe
        {
            device.vk.CmdBindDescriptorSets(handle, PipelineBindPoint.Graphics, pipelineLayout, 0u, 1u, descriptorSet, 0u, null);
        }
        return this;
    }

    public VkCommandBuffer PushConstant<T>(T constant, PipelineLayout layout, ShaderStageFlags stage) where T : unmanaged
    {
        unsafe
        {
            device.vk.CmdPushConstants(handle, layout, stage, 0u, (uint)sizeof(T), ref constant);
        }
        return this;
    }

    public VkCommandBuffer Draw(uint vertexCount, uint instanceCount = 1, uint fristIndex = 0, uint firstInstance = 0)
    {
        device.vk.CmdDraw(handle, vertexCount, instanceCount, fristIndex, firstInstance);
        return this;
    }

    public VkCommandBuffer DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
    {
        device.vk.CmdDrawIndexed(handle, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
        return this;
    }

    public VkCommandBuffer CopyBuffer(Buffer source, Buffer destination, in BufferCopy copyRegion)
    {
        device.vk.CmdCopyBuffer(handle, source, destination, 1u, in copyRegion);
        return this;
    }

    public VkCommandBuffer CopyBufferToImage(Buffer buffer, Image image, ImageLayout imageLayout, in BufferImageCopy region)
    {
        device.vk.CmdCopyBufferToImage(handle, buffer, image, imageLayout, 1u, in region);
        return this;
    }

    public VkCommandBuffer PipelineBarrier(PipelineStageFlags sourceStage, PipelineStageFlags destinationStage, ImageMemoryBarrier barrier)
    {
        unsafe
        {
            device.vk.CmdPipelineBarrier(handle, sourceStage, destinationStage, 0u, 0u, null, 0u, null, 1u, barrier);
        }
        return this;
    }

    public VkCommandBuffer BlitImage(Image sourceImage, ImageLayout soruceLayout, Image destinationImage, ImageLayout destinationLayout, ImageBlit blit, Filter filter)
    {
        device.vk.CmdBlitImage(handle, sourceImage, soruceLayout, destinationImage, destinationLayout, 1u, blit, filter);
        return this;
    }

    public void Dispose() => device.FreeCommandBuffer(this);
}
