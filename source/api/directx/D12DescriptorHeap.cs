namespace Vulkanoid.DirectX;

[Handle<ID3D12DescriptorHeap>]
public partial class D12DescriptorHeap
{
    public uint Size { get; init; }

    public uint IncrementSize { get; init; }

    public CpuDescriptorHandle CpuHandle => handle.GetCPUDescriptorHandleForHeapStart();
}
