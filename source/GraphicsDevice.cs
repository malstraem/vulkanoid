using Vulkanoid.Vulkan;

namespace Vulkanoid;

public abstract class GraphicsDevice
{
    [Obsolete("To do - true abstraction")]
    public static VkDevice CreateVulkan(Vk vk, string[] extensions) => new(vk, extensions);
}
