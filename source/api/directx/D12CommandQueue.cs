namespace Vulkanoid.DirectX;

[Handle<ID3D12CommandQueue>]
public partial class D12CommandQueue
{
    public void Execute(D12GraphicsCommandList list)
    {
        unsafe
        {
            var commandListsPtr = stackalloc ID3D12CommandList*[1]
{
                (ID3D12CommandList*)((ComPtr<ID3D12GraphicsCommandList>)list).Handle,
            };

            handle.ExecuteCommandLists(1u, commandListsPtr);
        }
    }

    public void Signal(D12Fence fence, ulong value) => ThrowHResult(handle.Signal<ID3D12Fence>(fence, value));
}
