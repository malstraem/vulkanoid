namespace Vulkanoid.DirectX;

public partial class D12Device
{
    public D12Resource CreateCommittedResource(in HeapProperties heapProperties, in ResourceDesc description, ResourceStates states)
    {
        unsafe
        {
            return new D12Resource(handle.CreateCommittedResource<ID3D12Resource>(in heapProperties, HeapFlags.None, in description, states, null), this);
        }
    }

    public void CreateRenderTargetView(D12Resource resource, CpuDescriptorHandle cpuHandle)
    {
        unsafe
        {
            handle.CreateRenderTargetView<ID3D12Resource>(resource, null, cpuHandle);
        }
    }

    public void CreateDepthStencilView(D12Resource resource, in DepthStencilViewDesc description, CpuDescriptorHandle cpuHandle)
        => handle.CreateDepthStencilView<ID3D12Resource>(resource, in description, cpuHandle);
}
