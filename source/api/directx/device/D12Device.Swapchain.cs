using Silk.NET.DXGI;

namespace Vulkanoid.DirectX;

public partial class D12Device
{
    public DXGISwapchain CreateSwapchainForHwnd(D12CommandQueue queue, nint hwnd, in SwapChainDesc1 description)
    {
        ComPtr<IDXGISwapChain1> swapchainHandle = null;

        unsafe
        {
            ThrowHResult(factory.CreateSwapChainForHwnd<ID3D12CommandQueue, IDXGIOutput, IDXGISwapChain1>(queue, hwnd, in description, null, default, ref swapchainHandle));
        }

        return new DXGISwapchain(swapchainHandle.QueryInterface<IDXGISwapChain>(), this);
    }
}
