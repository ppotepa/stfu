using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using STFU.NPR.Pipeline;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Common.Math;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Device;
using STFU.Rendering.DirectX.Upload;
using Vortice.Direct3D11;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxMeshWireframePass : IDisposable
{
    private readonly DirectXDevice _device;
    private readonly DirectXPipelineStates _states;
    private readonly DxMeshWireframeBuilder _builder;
    private readonly DxStrokeRasterPass _strokePass;
    private readonly ID3D11VertexShader _vertexShader;
    private readonly ID3D11PixelShader _pixelShader;
    private readonly ID3D11Buffer _frameConstants;
    private readonly Dictionary<ulong, EdgeBufferCacheEntry> _edgeBufferCache = [];
    private ID3D11Buffer? _vertexBuffer;
    private ID3D11ShaderResourceView? _vertexBufferSrv;
    private int _vertexCapacity;
    private bool _disposed;

    public DxMeshWireframePass(DirectXDevice device, DxMeshWireframeBuilder builder, DxStrokeRasterPass strokePass)
    {
        _device = device;
        _states = new DirectXPipelineStates(device.Device);
        _builder = builder;
        _strokePass = strokePass;

        var vertexShaderBytes = DirectXShaderCompiler.CompileFromFile("mesh_wireframe.hlsl", "VS", "vs_5_0");
        var pixelShaderBytes = DirectXShaderCompiler.CompileFromFile("mesh_wireframe.hlsl", "PS", "ps_5_0");

        using (_device.Lock())
        {
            unsafe
            {
                fixed (byte* vsPtr = vertexShaderBytes)
                fixed (byte* psPtr = pixelShaderBytes)
                {
                    _vertexShader = device.Device.CreateVertexShader(vsPtr, (nuint)vertexShaderBytes.Length);
                    _pixelShader = device.Device.CreatePixelShader(psPtr, (nuint)pixelShaderBytes.Length);
                }
            }
        }

        _frameConstants = device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = 48,
            BindFlags = BindFlags.ConstantBuffer,
            Usage = ResourceUsage.Default,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None,
            StructureByteStride = 0
        });
    }

    public void Execute(
        DirectXTextureResource target,
        NprRenderRequest request,
        NprRenderDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Quality.GpuMeshWireframePath == GpuMeshWireframePath.Stroke)
        {
            ExecuteStrokeFallback(target, request, diagnostics, cancellationToken);
            return;
        }

        var buildWatch = Stopwatch.StartNew();
        var batch = _builder.BuildGpuBatch(
            request.Scene,
            request.Assets,
            request.Analysis,
            request.Camera,
            request.Width,
            request.Height,
            request.Settings,
            request.Quality.MeshWireframeTopologyMode);
        buildWatch.Stop();

        diagnostics.PathCount = batch.Edges.Count;
        diagnostics.AddTiming(
            "GpuMeshBuild",
            buildWatch.Elapsed.TotalMilliseconds,
            $"native=1; topology={request.Quality.MeshWireframeTopologyMode}; edges={_builder.LastTopologyEdgeCount}; drawEdges={batch.Edges.Count}; vertices={batch.Vertices.Count}; path=native");

        if (batch.Vertices.Count == 0 || batch.Edges.Count == 0 || target.RenderTargetView is null)
        {
            return;
        }

        using (_device.Lock())
        {
            unsafe
            {
                var uploadWatch = Stopwatch.StartNew();
                var vertexUploadWatch = Stopwatch.StartNew();
                EnsureBufferCapacity(
                    ref _vertexBuffer,
                    ref _vertexBufferSrv,
                    ref _vertexCapacity,
                    batch.Vertices.Count,
                    DxMeshVertex.SizeInBytes);
                Upload(batch.Vertices, _vertexBuffer!, _vertexCapacity, DxMeshVertex.SizeInBytes);
                vertexUploadWatch.Stop();

                var edgeUploaded = false;
                var edgeUploadMs = 0.0;
                if (!_edgeBufferCache.TryGetValue(batch.EdgeSignature, out var edgeEntry) ||
                    edgeEntry.Count != batch.Edges.Count ||
                    edgeEntry.Capacity < batch.Edges.Count)
                {
                    var edgeUploadWatch = Stopwatch.StartNew();
                    edgeEntry?.Dispose();
                    var edgeBuffer = default(ID3D11Buffer);
                    var edgeSrv = default(ID3D11ShaderResourceView);
                    var edgeCapacity = 0;
                    EnsureBufferCapacity(
                        ref edgeBuffer,
                        ref edgeSrv,
                        ref edgeCapacity,
                        batch.Edges.Count,
                        DxMeshEdge.SizeInBytes);
                    Upload(batch.Edges, edgeBuffer!, edgeCapacity, DxMeshEdge.SizeInBytes);
                    edgeEntry = new EdgeBufferCacheEntry(edgeBuffer!, edgeSrv!, edgeCapacity, batch.Edges.Count);
                    _edgeBufferCache[batch.EdgeSignature] = edgeEntry;
                    edgeUploadWatch.Stop();
                    edgeUploadMs = edgeUploadWatch.Elapsed.TotalMilliseconds;
                    edgeUploaded = true;
                }

                UpdateConstants(request);
                uploadWatch.Stop();
                diagnostics.AddTiming(
                    "GpuMeshUpload",
                    uploadWatch.Elapsed.TotalMilliseconds,
                    $"vertices={batch.Vertices.Count}; edges={batch.Edges.Count}; vertexBytes={batch.Vertices.Count * DxMeshVertex.SizeInBytes}; edgeBytes={batch.Edges.Count * DxMeshEdge.SizeInBytes}; vertexUpload={vertexUploadWatch.Elapsed.TotalMilliseconds:0.###}; edgeUpload={edgeUploadMs:0.###}; edgeUploaded={(edgeUploaded ? 1 : 0)}; cacheHit={(edgeUploaded ? 0 : 1)}");

                var drawWatch = Stopwatch.StartNew();
                _device.Context.OMSetRenderTargets(target.RenderTargetView);
                _device.Context.RSSetViewport(0, 0, request.Width, request.Height);
                _device.Context.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
                _device.Context.IASetInputLayout(null);
                _device.Context.VSSetShader(_vertexShader);
                _device.Context.PSSetShader(_pixelShader);
                _device.Context.VSSetConstantBuffer(0, _frameConstants);
                _device.Context.VSSetShaderResource(0, _vertexBufferSrv);
                _device.Context.VSSetShaderResource(1, edgeEntry.Srv);
                _device.Context.OMSetBlendState(_states.PremultipliedAlphaBlend);
                _device.Context.RSSetState(_states.NoCullRasterizer);
                _device.Context.OMSetDepthStencilState(_states.DepthDisabled);
                _device.Context.DrawInstanced(6, (uint)batch.Edges.Count, 0, 0);
                _device.Context.VSSetShaderResource(0, null!);
                _device.Context.VSSetShaderResource(1, null!);
                drawWatch.Stop();
                diagnostics.AddTiming("GpuMeshWireframeDraw", drawWatch.Elapsed.TotalMilliseconds, $"edges={batch.Edges.Count}");
            }
        }
    }

    private void ExecuteStrokeFallback(
        DirectXTextureResource target,
        NprRenderRequest request,
        NprRenderDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        var buildWatch = Stopwatch.StartNew();
        var segments = _builder.BuildSegments(
            request.Scene,
            request.Assets,
            request.Analysis,
            request.Camera,
            request.Width,
            request.Height,
            request.Settings,
            request.Theme,
            request.Quality.MeshWireframeTopologyMode);
        buildWatch.Stop();

        diagnostics.PathCount = segments.Count;
        diagnostics.AddTiming(
            "GpuMeshBuild",
            buildWatch.Elapsed.TotalMilliseconds,
            $"native=0; topology={request.Quality.MeshWireframeTopologyMode}; edges={_builder.LastTopologyEdgeCount}; drawEdges={segments.Count}; vertices=0; path=stroke");

        _strokePass.Execute(target, request, segments, 1f, diagnostics, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var entry in _edgeBufferCache.Values)
        {
            entry.Dispose();
        }

        _edgeBufferCache.Clear();
        _vertexBufferSrv?.Dispose();
        _vertexBuffer?.Dispose();
        _frameConstants.Dispose();
        _pixelShader.Dispose();
        _vertexShader.Dispose();
        _states.Dispose();
    }

    private void UpdateConstants(NprRenderRequest request)
    {
        var color = request.Theme.MeshStrokeColor;
        var constants = new MeshFrameConstants(
            new Vector4(
                request.Width,
                request.Height,
                NumericMath.InverseAtLeast(request.Width),
                NumericMath.InverseAtLeast(request.Height)),
            new Vector4(
                NumericMath.AtLeast(request.Quality.GpuStrokeCoverageSoftness, 0.25f),
                0f,
                0f,
                0f),
            new Vector4(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                0.55f));

        unsafe
        {
            _device.Context.UpdateSubresource(_frameConstants, 0, null, (IntPtr)(&constants), 0, 0);
        }
    }

    private void EnsureBufferCapacity(
        ref ID3D11Buffer? buffer,
        ref ID3D11ShaderResourceView? srv,
        ref int capacity,
        int requiredCount,
        int elementSize)
    {
        if (buffer is not null && requiredCount <= capacity)
        {
            return;
        }

        srv?.Dispose();
        buffer?.Dispose();

        capacity = NextCapacity(requiredCount);
        buffer = _device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = (uint)(capacity * elementSize),
            BindFlags = BindFlags.ShaderResource,
            Usage = ResourceUsage.Dynamic,
            CPUAccessFlags = CpuAccessFlags.Write,
            MiscFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = (uint)elementSize
        });
        srv = CreateStructuredBufferSrv(buffer, capacity);
    }

    private unsafe void Upload<T>(List<T> values, ID3D11Buffer buffer, int capacity, int elementSize)
        where T : unmanaged
    {
        var mapped = _device.Context.Map(buffer, 0, MapMode.WriteDiscard, MapFlags.None);
        try
        {
            var span = CollectionsMarshal.AsSpan(values);
            fixed (T* source = span)
            {
                Buffer.MemoryCopy(
                    source,
                    mapped.DataPointer.ToPointer(),
                    (long)capacity * elementSize,
                    (long)values.Count * elementSize);
            }
        }
        finally
        {
            _device.Context.Unmap(buffer, 0);
        }
    }

    private ID3D11ShaderResourceView CreateStructuredBufferSrv(ID3D11Buffer buffer, int elementCount)
    {
        var desc = new ShaderResourceViewDescription(
            buffer,
            Vortice.DXGI.Format.Unknown,
            0,
            (uint)elementCount,
            BufferExtendedShaderResourceViewFlags.None);

        return _device.Device.CreateShaderResourceView(buffer, desc);
    }

    private static int NextCapacity(int required)
    {
        var capacity = 4;
        while (capacity < required)
        {
            capacity <<= 1;
        }

        return capacity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct MeshFrameConstants(
        Vector4 ViewportInv,
        Vector4 CoverageSoftness,
        Vector4 StrokeColor);

    private sealed class EdgeBufferCacheEntry(
        ID3D11Buffer buffer,
        ID3D11ShaderResourceView srv,
        int capacity,
        int count) : IDisposable
    {
        public ID3D11Buffer Buffer { get; } = buffer;

        public ID3D11ShaderResourceView Srv { get; } = srv;

        public int Capacity { get; } = capacity;

        public int Count { get; } = count;

        public void Dispose()
        {
            Srv.Dispose();
            Buffer.Dispose();
        }
    }
}
