using Silk.NET.DXGI;

namespace Vulkanoid.DirectX;

[Handle<IDXGISwapChain>]
public partial class DXGISwapchain
{
    [Obsolete("add flexibility")]
    public void Present() => ThrowHResult(handle.Present(1u, 0u));

    public D12Resource GetBuffer(uint index) => new(handle.GetBuffer<ID3D12Resource>(index), device);

    public uint GetCurrentBackBufferIndex() => handle.QueryInterface<IDXGISwapChain3>().GetCurrentBackBufferIndex();
}
