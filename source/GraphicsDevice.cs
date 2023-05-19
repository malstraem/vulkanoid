using Silk.NET.Core.Contexts;

using Vulkanoid.Vulkan;

namespace Vulkanoid;

public abstract class GraphicsDevice
{
    [Obsolete("To do - true abstraction")]
    public static VkDevice CreateVulkan(IVkSurface surface) => new(surface);
}