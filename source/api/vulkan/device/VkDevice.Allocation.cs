namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    internal CommandBuffer AllocateCommandBuffer(in CommandBufferAllocateInfo info)
    {
        vk.AllocateCommandBuffers(handle, in info, out var commandBufferHandle).Check();

        return commandBufferHandle;
    }

    internal VkDescriptorSet AllocateDescriptorSet(in DescriptorSetAllocateInfo allocateInfo)
    {
        vk.AllocateDescriptorSets(handle, in allocateInfo, out var setHandle).Check();

        return new VkDescriptorSet(setHandle, this);
    }
}
