namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    public void FreeCommandBuffer(VkCommandBuffer commandBuffer) => vk.FreeCommandBuffers(handle, commandBuffer.commandPool, 1u, commandBuffer);
}