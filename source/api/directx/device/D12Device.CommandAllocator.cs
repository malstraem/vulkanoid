namespace Vulkanoid.DirectX;

public partial class D12Device
{
    [Obsolete("add flexibility")]
    public D12CommandAllocator CreateCommandAllocator()
    {
        return new D12CommandAllocator(handle.CreateCommandAllocator<ID3D12CommandAllocator>(CommandListType.Direct), this);
    }
}
