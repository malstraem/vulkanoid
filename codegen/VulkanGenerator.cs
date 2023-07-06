using Microsoft.CodeAnalysis;

namespace Vulkanoid.Generators;

[Generator]
public class VulkanGenerator : ApiGenerator
{
    public VulkanGenerator()
    {
        deviceTypeName = "VkDevice";
        @namespace = "Vulkanoid.Vulkan";
    }
}
