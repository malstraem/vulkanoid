using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Vulkanoid.Vulkan;

[Handle<SwapchainKHR>]
public sealed partial class VkSwapchain : IDisposable
{
    private readonly KhrSwapchain swapchainExt;

    private readonly VkQueue presentQueue;

    //private readonly SwapchainCreateInfoKHR info;

    public VkSwapchain(SwapchainKHR handle, /*in SwapchainCreateInfoKHR info,*/ VkDevice device, VkQueue presentQueue, KhrSwapchain swapchainExt) 
        : this(handle, device)
    {
        //this.info = info;
        this.presentQueue = presentQueue;
        this.swapchainExt = swapchainExt;
    }

    public Result Present(Semaphore signalSemaphore, uint imageIndex)
    {
        unsafe
        {
            fixed (SwapchainKHR* handlePtr = &handle)
            {
                var presentInfo = new PresentInfoKHR(
                    waitSemaphoreCount: 1u,
                    pWaitSemaphores: &signalSemaphore,
                    swapchainCount: 1u,
                    pSwapchains: handlePtr,
                    pImageIndices: &imageIndex);

                return swapchainExt.QueuePresent(presentQueue, in presentInfo);
            }
        }
    }

    public Result AcquireNextImage(Semaphore imageAvailableSemaphore, out uint imageIndex)
    {
        imageIndex = default;

        unsafe
        {
            return swapchainExt.AcquireNextImage(device, handle, ulong.MaxValue, imageAvailableSemaphore, default, ref imageIndex);
        }
    }

    public required Extent2D Extent { get; init; }

    public required VkFramebuffer[] Framebuffers { get; init; }

    public void Dispose() => device.DestroySwapchain(handle);
}
