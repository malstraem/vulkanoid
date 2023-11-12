namespace Vulkanoid.DirectX;

public partial class D12Device
{
    [Obsolete("add flexibility")]
    public D12GraphicsCommandList CreateGraphicsList(D12CommandAllocator allocator)
    {
        var listHandle = handle.CreateCommandList
            <ID3D12CommandAllocator, ID3D12PipelineState, ID3D12GraphicsCommandList>(nodeMask: 0, CommandListType.Direct, allocator, null);

        ThrowHResult(listHandle.Close());

        return new D12GraphicsCommandList(listHandle, this);
    }
}
