namespace Vulkanoid.Vulkan;

[Handle<DescriptorPool>]
public sealed partial class VkDescriptorPool : IDisposable
{
    [Obsolete("Todo - add flexibility")]
    public VkDescriptorSet CreateDescriptorSet<T>(
        VkDescriptorSetLayout descriptorSetLayout,
        VkBuffer uniformBuffer,
        VkImageView imageView, 
        VkSampler sampler) where T : unmanaged
    {
        unsafe
        {
            var descriptorSetLayoutHandle = (DescriptorSetLayout)descriptorSetLayout;
            var layoutsPtr = &descriptorSetLayoutHandle;

            var allocateInfo = new DescriptorSetAllocateInfo(descriptorPool: handle, descriptorSetCount: 1u, pSetLayouts: layoutsPtr);

            var descriptorSet = device.AllocateDescriptorSet(in allocateInfo);

            var imageInfo = new DescriptorImageInfo(sampler, imageView, ImageLayout.ShaderReadOnlyOptimal);

            var bufferInfo = new DescriptorBufferInfo(uniformBuffer, 0u, (ulong)sizeof(T));

            Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[2];

            descriptorWrites[0] = new(dstSet: descriptorSet, dstBinding: 0u, dstArrayElement: 0u, descriptorType: DescriptorType.UniformBuffer, descriptorCount: 1u, pBufferInfo: &bufferInfo);
            descriptorWrites[1] = new(dstSet: descriptorSet, dstBinding: 1u, dstArrayElement: 0u, descriptorType: DescriptorType.CombinedImageSampler, descriptorCount: 1u, pImageInfo: &imageInfo);

            device.UpdateDescriptorSet(descriptorWrites);

            return descriptorSet;
        }
    }

    public void Dispose() => throw new NotImplementedException();
}
