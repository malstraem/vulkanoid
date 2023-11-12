using Silk.NET.DXGI;

using BlendOp = Silk.NET.Direct3D12.BlendOp;
using Format = Silk.NET.DXGI.Format;
using StencilOp = Silk.NET.Direct3D12.StencilOp;

namespace Vulkanoid.DirectX;

public partial class D12Device
{
    [Obsolete("add flexibility")]
    public D12Pipeline CreatePipeline()
    {
        using ComPtr<ID3D10Blob> pixelShader = null;
        using ComPtr<ID3D10Blob> vertexShader = null;

        var rootSignature = (ComPtr<ID3D12RootSignature>)CreateRootSignature();

        unsafe
        {
            fixed (char* fileName = Path.Combine(AppContext.BaseDirectory, "shader.hlsl"))
            {
                long entryPoint = 0x00006E69614D5356;
                long target = 0x0000305F355F7376;
                ID3D10Blob* errorMsgs;

                ThrowHResult(compiler.CompileFromFile(fileName, pDefines: null, pInclude: null, (byte*)&entryPoint, (byte*)&target,
                    0u, Flags2: 0, vertexShader.GetAddressOf(), ppErrorMsgs: &errorMsgs));

                entryPoint = 0x00006E69614D5350; // PSMain
                target = 0x0000305F355F7370; // ps_5_0
                ThrowHResult
                (
                    compiler.CompileFromFile
                    (
                        fileName, pDefines: null, pInclude: null, (byte*)&entryPoint, (byte*)&target,
                        0u, Flags2: 0, pixelShader.GetAddressOf(), ppErrorMsgs: &errorMsgs
                    )
                );
            }
        }

        const int InputElementDescsCount = 2;

        unsafe
        {
            ulong* semanticName0 = stackalloc ulong[2]
            {
                0x4E4F495449534F50,
                0x0000000000000000,
            };

            ulong* semanticName1 = stackalloc ulong[1]
            {
                0x000000524F4C4F43,
            };

            var inputElementDescs = stackalloc InputElementDesc[InputElementDescsCount]
            {
                new InputElementDesc
                {
                    SemanticName = (byte*)semanticName0,
                    Format = Format.FormatR32G32B32Float,
                    InputSlotClass = InputClassification.PerVertexData,
                },
                new InputElementDesc
                {
                    SemanticName = (byte*)semanticName1,
                    Format = Format.FormatR32G32B32A32Float,
                    AlignedByteOffset = 12,
                    InputSlotClass = InputClassification.PerVertexData,
                },
            };

            var defaultRenderTargetBlend = new RenderTargetBlendDesc()
            {
                BlendEnable = 0,
                LogicOpEnable = 0,
                SrcBlend = Blend.One,
                DestBlend = Blend.Zero,
                BlendOp = BlendOp.Add,
                SrcBlendAlpha = Blend.One,
                DestBlendAlpha = Blend.Zero,
                BlendOpAlpha = BlendOp.Add,
                LogicOp = Silk.NET.Direct3D12.LogicOp.Noop,
                RenderTargetWriteMask = (byte)ColorWriteEnable.All
            };

            var defaultStencilOp = new DepthStencilopDesc
            {
                StencilFailOp = StencilOp.Keep,
                StencilDepthFailOp = StencilOp.Keep,
                StencilPassOp = StencilOp.Keep,
                StencilFunc = ComparisonFunc.Always
            };

            var psoDesc = new GraphicsPipelineStateDesc
            {
                InputLayout = new InputLayoutDesc
                {
                    PInputElementDescs = inputElementDescs,
                    NumElements = InputElementDescsCount,
                },
                PRootSignature = rootSignature,
                VS = new ShaderBytecode(vertexShader.Get().GetBufferPointer(), vertexShader.Get().GetBufferSize()),
                PS = new ShaderBytecode(pixelShader.Get().GetBufferPointer(), pixelShader.Get().GetBufferSize()),
                RasterizerState = new RasterizerDesc
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.Back,
                    FrontCounterClockwise = 0,
                    DepthBias = D3D12.DefaultDepthBias,
                    DepthBiasClamp = 0,
                    SlopeScaledDepthBias = 0,
                    DepthClipEnable = 1,
                    MultisampleEnable = 0,
                    AntialiasedLineEnable = 0,
                    ForcedSampleCount = 0,
                    ConservativeRaster = ConservativeRasterizationMode.Off,
                },
                BlendState = new BlendDesc
                {
                    AlphaToCoverageEnable = 0,
                    IndependentBlendEnable = 0,
                    RenderTarget = new BlendDesc.RenderTargetBuffer()
                    {
                        [0] = defaultRenderTargetBlend,
                        [1] = defaultRenderTargetBlend,
                        [2] = defaultRenderTargetBlend,
                        [3] = defaultRenderTargetBlend,
                        [4] = defaultRenderTargetBlend,
                        [5] = defaultRenderTargetBlend,
                        [6] = defaultRenderTargetBlend,
                        [7] = defaultRenderTargetBlend
                    }
                },
                DepthStencilState = new DepthStencilDesc
                {
                    DepthEnable = 1,
                    DepthWriteMask = DepthWriteMask.All,
                    DepthFunc = ComparisonFunc.Less,
                    StencilEnable = 0,
                    StencilReadMask = D3D12.DefaultStencilReadMask,
                    StencilWriteMask = D3D12.DefaultStencilWriteMask,
                    FrontFace = defaultStencilOp,
                    BackFace = defaultStencilOp
                },
                SampleMask = uint.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                NumRenderTargets = 1,
                SampleDesc = new SampleDesc(count: 1, quality: 0),
            };
            psoDesc.DepthStencilState.DepthEnable = 0;
            psoDesc.RTVFormats[0] = Format.FormatR8G8B8A8Unorm;

            return new D12Pipeline(handle.CreateGraphicsPipelineState<ID3D12PipelineState>(in psoDesc), this);
        }
    }
}
