namespace Vulkanoid.DirectX;

public partial class D12Device
{
    [Obsolete("add flexibility")]
    public D12CommandQueue CreateQueue()
    {
        return new D12CommandQueue(handle.CreateCommandQueue<ID3D12CommandQueue>(new CommandQueueDesc()), this);
    }
}
