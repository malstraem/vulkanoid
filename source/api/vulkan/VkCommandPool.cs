namespace Vulkanoid.Vulkan;

[Handle<CommandPool>]
public sealed partial class VkCommandPool : IDisposable
{
    public VkCommandBuffer CreateCommandBuffer(CommandBufferUsageFlags usage = CommandBufferUsageFlags.OneTimeSubmitBit)
    {
        unsafe
        {
            var allocateInfo = new CommandBufferAllocateInfo(level: CommandBufferLevel.Primary, commandPool: handle, commandBufferCount: 1u);
            var commandBufferHandle = device.AllocateCommandBuffer(allocateInfo);

            var beginInfo = new CommandBufferBeginInfo(flags: usage);
            device.vk.BeginCommandBuffer(commandBufferHandle, beginInfo).Check();

            return new VkCommandBuffer(commandBufferHandle, device, this);
        }
    }

    public void Dispose() => throw new NotImplementedException();
}
