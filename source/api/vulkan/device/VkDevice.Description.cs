namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    [Obsolete("add flexibility")]
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

            vk.CreateDescriptorPool(handle, poolInfo, null, out var poolHandle).Check();

            return new VkDescriptorPool(poolHandle, this);
        }
    }

    [Obsolete("add flexibility")]
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

            vk.CreateDescriptorSetLayout(handle, in createInfo, null, out var setLayoutHandle).Check();

            return new VkDescriptorSetLayout(setLayoutHandle, this);
        }
    }

    public void UpdateDescriptorSet(ReadOnlySpan<WriteDescriptorSet> writeDescriptorSets) => vk.UpdateDescriptorSets(handle, writeDescriptorSets, default);
}
