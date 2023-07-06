using Silk.NET.Vulkan;

namespace Vulkanoid.Tests;

public class VkDeviceTests
{
    [Fact]
    public void CreateVulkan()
    {
        var device = GraphicsDevice.CreateVulkan(Vk.GetApi(), new string[0]);

        Assert.True(((Device)device).Handle != nint.Zero);
    }
}
