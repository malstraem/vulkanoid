using System.Numerics;
using System.Runtime.CompilerServices;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using Silk.NET.Windowing;

using Vulkanoid.DirectX;

using static Silk.NET.Core.Native.SilkMarshal;

namespace Vulkanoid.Sample.DirectX;

public unsafe class DirectRenderer
{
    private Format backBufferFormat = Format.FormatR8G8B8A8Unorm;
    private Format depthBufferFormat = Format.FormatD32Float;

    private Vector4 backgroundColor = Vector4.Zero;

    private uint frameCount = 2u;
    private uint frameIndex;

    private IWindow window;

    private D12Device device = new();

    private DXGISwapchain swapchain;

    private D12Resource[] renderTargets = new D12Resource[2];

    private D12Resource depthStencil;

    private D12DescriptorHeap[] renderTargetHeaps;
    private D12DescriptorHeap dsvHeap;

    private D12CommandQueue commandQueue;

    private D12CommandAllocator commandAllocator;

    private D12GraphicsCommandList graphicsCommandList;

    private D12RootSignature rootSignature;
    private D12Pipeline pipelineState;

    private D12Fence fence;

    private ComPtr<ID3D12Resource> vertexBuffer;
    private VertexBufferView vertexBufferView;

    private Viewport viewport;
    private Box2D<int> scissorRect;

    public DirectRenderer(IWindow window)
    {
        this.window = window;

        renderTargets = new D12Resource[2];
        renderTargetHeaps = new D12DescriptorHeap[2];
    }

    public struct Vertex
    {
        public Vector3 Position;

        public Vector4 Color;
    }

    public void OnRender(double _)
    {
        commandQueue.Signal(fence, 0u);

        frameIndex = swapchain.GetCurrentBackBufferIndex();

        commandAllocator.Reset();

        graphicsCommandList.Reset(commandAllocator, pipelineState);
        graphicsCommandList.SetRootSignature(rootSignature);
        graphicsCommandList.SetViewport(viewport);
        graphicsCommandList.SetScissorRect(scissorRect);

        graphicsCommandList.ClearRenderTargetView(renderTargetHeaps[frameIndex], backgroundColor);

        graphicsCommandList.ClearDepthStencilView(dsvHeap);

        graphicsCommandList.OMSetRenderTarget(renderTargetHeaps[frameIndex], dsvHeap);

        graphicsCommandList.SetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        graphicsCommandList.SetVertexBuffer(vertexBufferView);
        graphicsCommandList.DrawInstanced(3u, 1u, 0u, 0u);

        graphicsCommandList.Close();

        commandQueue.Execute(graphicsCommandList);

        swapchain.Present();
    }

    private void CreateAssets()
    {
        const int TriangleVerticesCount = 3;

        var triangleVertices = stackalloc Vertex[TriangleVerticesCount]
        {
            new Vertex
            {
                Position = new Vector3(0.0f, 0.5f, 0.0f),
                Color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f)
            },
            new Vertex
            {
                Position = new Vector3(0.5f, -0.5f, 0.0f),
                Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f)
            },
            new Vertex
            {
                Position = new Vector3(-0.5f, -0.5f, 0.0f),
                Color = new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
            },
        };

        uint vertexBufferSize = (uint)sizeof(Vertex) * TriangleVerticesCount;

        var heapProperties = new HeapProperties(HeapType.Upload);

        var resourceDesc = new ResourceDesc(
            ResourceDimension.Buffer,
            0,
            vertexBufferSize,
            1,
            1,
            1,
            Format.FormatUnknown,
            new SampleDesc(1, 0),
            TextureLayout.LayoutRowMajor,
            ResourceFlags.None);

        vertexBuffer = device.CreateCommittedResource(in heapProperties, in resourceDesc, ResourceStates.GenericRead);

        var readRange = new Silk.NET.Direct3D12.Range();

        void* pVertexDataBegin = null;
        ThrowHResult(vertexBuffer.Map(0u, in readRange, ref pVertexDataBegin));
        Unsafe.CopyBlock(pVertexDataBegin, triangleVertices, vertexBufferSize);
        Silk.NET.Direct3D12.Range* writeRange = null;
        vertexBuffer.Unmap(0u, writeRange);

        vertexBufferView.BufferLocation = vertexBuffer.GetGPUVirtualAddress();
        vertexBufferView.StrideInBytes = (uint)sizeof(Vertex);
        vertexBufferView.SizeInBytes = vertexBufferSize;

        commandQueue.Execute(graphicsCommandList);
    }

    private void CreateDescriptorHeaps()
    {
        var rtvHeapDesc = new DescriptorHeapDesc
        {
            NumDescriptors = frameCount,
            Type = DescriptorHeapType.Rtv
        };

        for (uint i = 0u; i < frameCount; i++)
            renderTargetHeaps[i] = device.CreateDescriptorHeap(in rtvHeapDesc);

        var dsvHeapDesc = new DescriptorHeapDesc
        {
            NumDescriptors = 1u,
            Type = DescriptorHeapType.Dsv
        };

        dsvHeap = device.CreateDescriptorHeap(in dsvHeapDesc);
    }

    private void CreateDeviceDependentResources()
    {
        commandQueue = device.CreateQueue();

        CreateDescriptorHeaps();

        commandAllocator = device.CreateCommandAllocator();

        fence = device.CreateFence();

        rootSignature = device.CreateRootSignature();

        pipelineState = device.CreatePipeline();

        graphicsCommandList = device.CreateGraphicsList(commandAllocator);

        CreateAssets();
    }

    void CreateResourceViews()
    {
        for (int i = 0; i < frameCount; i++)
            renderTargets[i] = swapchain.GetBuffer((uint)i);

        var heapProperties = new HeapProperties(HeapType.Default);

        var resourceDesc = new ResourceDesc
        (
            ResourceDimension.Texture2D,
            0ul,
            (ulong)window.Size.X,
            (uint)window.Size.Y,
            1,
            1,
            depthBufferFormat,
            new SampleDesc { Count = 1, Quality = 0 },
            TextureLayout.LayoutUnknown,
            ResourceFlags.AllowDepthStencil
        );

        depthStencil = device.CreateCommittedResource(in heapProperties, in resourceDesc, ResourceStates.DepthWrite);

        var dsvDesc = new DepthStencilViewDesc
        {
            Format = depthBufferFormat,
            ViewDimension = DsvDimension.Texture2D
        };

        device.CreateDepthStencilView(depthStencil, in dsvDesc, dsvHeap.CpuHandle);

        for (uint i = 0u; i < frameCount; i++)
            device.CreateRenderTargetView(renderTargets[i], renderTargetHeaps[i].CpuHandle);
    }

    void CreateWindowSizeDependentResources()
    {
        commandQueue.Signal(fence, 0u);

        var description = new SwapChainDesc1
        {
            BufferCount = frameCount,
            Width = (uint)window.Size.X,
            Height = (uint)window.Size.Y,
            Format = backBufferFormat,
            BufferUsage = DXGI.UsageRenderTargetOutput,
            SwapEffect = SwapEffect.FlipDiscard,
            SampleDesc = new(1, 0),
        };

        nint hwnd = window.Native.DXHandle.Value;

        swapchain = device.CreateSwapchainForHwnd(commandQueue, hwnd, in description);

        CreateResourceViews();

        viewport = new Viewport
        {
            Width = window.Size.X,
            Height = window.Size.Y,
            MaxDepth = 1
        };

        scissorRect = new(Vector2D<int>.Zero, window.Size);
    }

    void DestroyDeviceDependentResources()
    {
        /*rootSignature.Release();

        if (fenceEvent != IntPtr.Zero)
            _ = CloseWindowsHandle(fenceEvent);

        fence.Release();

        commandAllocator.Release();

        dsvHeap.Release();
        rtvHeap.Release();
        commandQueue.Release();
        _ = device.Release();
        adapter.Release();
        factory.Release();*/
    }

    void DestroyWindowSizeDependentResources()
    {
        //depthStencil.Release();

        /*for (int i = 0; i < renderTargets.Length; i++)
            _ = renderTargets[i].Release();

        swapchain.Release();*/
    }

    public void OnDestroy()
    {
        DestroyWindowSizeDependentResources();
        DestroyDeviceDependentResources();
    }

    public void OnLoad()
    {
        CreateDeviceDependentResources();
        CreateWindowSizeDependentResources();
    }

    public void OnWindowSizeChanged(Vector2D<int> size)
    {
        if (size != window.Size)
        {
            DestroyWindowSizeDependentResources();
            CreateWindowSizeDependentResources();
        }
    }
}
