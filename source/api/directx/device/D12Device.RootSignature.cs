namespace Vulkanoid.DirectX;

public partial class D12Device
{
    [Obsolete("add flexibility")]
    public D12RootSignature CreateRootSignature()
    {
        ComPtr<ID3D10Blob> signature = null;
        ComPtr<ID3D10Blob> error = null;

        var rootSignatureDesc = new RootSignatureDesc
        {
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
        };

        ThrowHResult(d3d12.SerializeRootSignature(in rootSignatureDesc, D3DRootSignatureVersion.Version1, ref signature, ref error));

        unsafe
        {
            return new D12RootSignature(handle.CreateRootSignature<ID3D12RootSignature>(0u, signature.GetBufferPointer(), signature.GetBufferSize()), this);
        }
    }
}
