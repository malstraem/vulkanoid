namespace Vulkanoid.Vulkan;

internal static class VkFormatExtensions
{
    internal static bool HasStencil(this Format format) => format is Format.D32SfloatS8Uint or Format.D24UnormS8Uint;
}