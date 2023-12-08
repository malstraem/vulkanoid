namespace Vulkanoid.DirectX;

[Handle<ID3D12CommandAllocator>]
public partial class D12CommandAllocator
{
    [Obsolete("add flexibility")]
    public D12GraphicsCommandList CreateGraphicsList() => device.CreateGraphicsList(this);

    public void Reset() => ThrowHResult(handle.Reset());
}
