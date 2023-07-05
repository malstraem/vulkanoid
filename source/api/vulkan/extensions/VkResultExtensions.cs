using System.Diagnostics;

namespace Vulkanoid.Vulkan;

public static class VkResultExtensions
{
    [DebuggerHidden]
    public static void Check(this Result result)
    {
        if (result is not Result.Success)
            throw new Exception($"vulkan call failed - {result}");
    }
}
