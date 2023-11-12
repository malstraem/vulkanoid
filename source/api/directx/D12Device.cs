using Silk.NET.Direct3D.Compilers;
using Silk.NET.DXGI;

namespace Vulkanoid.DirectX;

public partial class D12Device
{
    private readonly DXGI dxgi = DXGI.GetApi(null);
    private readonly D3D12 d3d12 = D3D12.GetApi();
    private readonly D3DCompiler compiler = D3DCompiler.GetApi();

    private readonly ComPtr<IDXGIFactory4> factory;
    private readonly ComPtr<IDXGIAdapter1> adapter;

    private readonly ComPtr<ID3D12Device> handle;

    public D12Device()
    {
        factory = dxgi.CreateDXGIFactory2<IDXGIFactory4>(0u);

        // DXGI_ERROR_NOT_FOUND - 0x887A0002
        for (uint adapterIndex = 0u; factory.EnumAdapters1(adapterIndex, ref adapter) != unchecked((int)0x887A0002); ++adapterIndex)
        {
            AdapterDesc1 desc = default;

            ThrowHResult(adapter.GetDesc1(ref desc));

            if ((desc.Flags & (uint)AdapterFlag.Software) != 0)
                continue;

            if (HResult.IndicatesSuccess(d3d12.CreateDevice(adapter, D3DFeatureLevel.Level120, out handle)))
                break;
        }

        handle = d3d12.CreateDevice<IDXGIAdapter1, ID3D12Device>(adapter, D3DFeatureLevel.Level120);
    }
}
