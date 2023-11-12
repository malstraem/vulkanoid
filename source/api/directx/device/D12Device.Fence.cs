namespace Vulkanoid.DirectX;

public partial class D12Device
{
    [Obsolete("add flexibility")]
    public D12Fence CreateFence() => new(handle.CreateFence<ID3D12Fence>(0, FenceFlags.None), this);
}
