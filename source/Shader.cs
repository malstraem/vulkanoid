using Vulkanoid.Vulkan;

namespace Vulkanoid;

[Obsolete("add true abstraction")]
public class Shader
{
    public VkShaderModule Module { get; init; }

    public ShaderStageFlags Stage { get; init; }

    public string EntryPoint { get; init; }
}
