using Vulkanoid.Vulkan;

namespace Vulkanoid;

public abstract class GraphicsDevice
{
    [Obsolete("add true abstraction")]
    public static VkDevice CreateVulkan(Vk vk, string[] extensions) => new(vk, extensions);
}
