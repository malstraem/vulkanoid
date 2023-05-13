using Vulkanoid.Vulkan;

namespace Vulkanoid;

public abstract class GraphicsDevice
{
    public static GraphicsDevice CreateVulkan(SurfaceKHR? surface = null) => new VkDevice(surface);
}