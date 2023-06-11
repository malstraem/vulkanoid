namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
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
}