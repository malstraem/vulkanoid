using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Vulkanoid.Vulkan;

[Handle<SwapchainKHR>]
public sealed partial class VkSwapchain : IDisposable
{
    private readonly KhrSwapchain swapchainExt;

    private readonly VkQueue presentQueue;

    public VkSwapchain(SwapchainKHR handle, Extent2D extent, VkFramebuffer[] framebuffers, VkQueue presentQueue, KhrSwapchain swapchainExt, VkDevice device) 
        : this(handle, device)
    {
        Extent = extent;
        Framebuffers = framebuffers;
        
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

                return swapchainExt.QueuePresent(presentQueue, &presentInfo);
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

    public Extent2D Extent { get; }

    public VkFramebuffer[] Framebuffers { get; }

    public void Dispose() => throw new NotImplementedException();
}