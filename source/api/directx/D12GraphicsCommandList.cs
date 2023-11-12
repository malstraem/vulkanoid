using System.Numerics;

using Silk.NET.Maths;

using Viewport = Silk.NET.Direct3D12.Viewport;

namespace Vulkanoid.DirectX;

[Handle<ID3D12GraphicsCommandList>]
public partial class D12GraphicsCommandList
{
    public void Reset(D12CommandAllocator allocator, D12Pipeline pipeline)
        => ThrowHResult(handle.Reset<ID3D12CommandAllocator, ID3D12PipelineState>(allocator, pipeline));

    public void SetPrimitiveTopology(D3DPrimitiveTopology topology) => handle.IASetPrimitiveTopology(topology);

    public void SetVertexBuffer(in VertexBufferView vertexBufferView) => handle.IASetVertexBuffers(0u, 1u, in vertexBufferView);

    public void DrawInstanced(uint vertexCountPerInstance, uint instanceCount, uint startVertexLocation, uint startInstanceLocation)
        => handle.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);

    public void SetRootSignature(D12RootSignature signature) => handle.SetComputeRootSignature<ID3D12RootSignature>(signature);

    public void SetViewport(Viewport viewport) => handle.RSSetViewports(1u, in viewport);

    public void SetScissorRect(Box2D<int> rect) => handle.RSSetScissorRects(1u, in rect);

    public void ClearRenderTargetView(D12DescriptorHeap descriptorHeap, Vector4 rgba)
    {
        unsafe
        {
            Box2D<int>* pRect = null;

            handle.ClearRenderTargetView(descriptorHeap.CpuHandle, (float*)&rgba, 0u, pRect);
        }
    }

    public void ClearDepthStencilView(D12DescriptorHeap descriptorHeap)
    {
        unsafe
        {
            Box2D<int>* pRect = null;
            handle.ClearDepthStencilView(descriptorHeap.CpuHandle, ClearFlags.Depth, 1f, 0, 0u, pRect);
        }
    }

    public void OMSetRenderTarget(D12DescriptorHeap renderDescriptorHeap, D12DescriptorHeap depthStencilDescriptorHeap)
    {
        var cpuHandle = renderDescriptorHeap.CpuHandle;
        var depthCpuHandle = depthStencilDescriptorHeap.CpuHandle;

        handle.OMSetRenderTargets(1u, in cpuHandle, 0u, in depthCpuHandle);
    }

    public void Close() => ThrowHResult(handle.Close());
}
