namespace Vulkanoid.DirectX;

public partial class D12Device
{
    public D12DescriptorHeap CreateDescriptorHeap(in DescriptorHeapDesc description)
    {
        return new D12DescriptorHeap(handle.CreateDescriptorHeap<ID3D12DescriptorHeap>(in description), this)
        {
            Size = handle.GetDescriptorHandleIncrementSize(description.Type),
            IncrementSize = handle.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv)
        };
    }
}
