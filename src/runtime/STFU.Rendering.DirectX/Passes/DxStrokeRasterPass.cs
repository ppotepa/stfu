using System.Diagnostics;
using System.Runtime.InteropServices;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Device;
using STFU.Rendering.DirectX.Upload;
using STFU.Strokes;
using Vortice.Direct3D11;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxStrokeRasterPass : IDisposable
{
    private readonly DirectXDevice _device;
    private readonly DirectXPipelineStates _states;
    private readonly ID3D11VertexShader _vertexShader;
    private readonly ID3D11PixelShader _pixelShader;
    private readonly ID3D11Buffer _frameConstants;
    private bool _disposed;

    public DxStrokeRasterPass(DirectXDevice device)
    {
        _device = device;
        _states = new DirectXPipelineStates(device.Device);

        var vertexShaderBytes = DirectXShaderCompiler.CompileFromFile("stroke_raster.hlsl", "VS", "vs_5_0");
        var pixelShaderBytes = DirectXShaderCompiler.CompileFromFile("stroke_raster.hlsl", "PS", "ps_5_0");

        unsafe
        {
            fixed (byte* vsPtr = vertexShaderBytes)
            fixed (byte* psPtr = pixelShaderBytes)
            {
                _vertexShader = device.Device.CreateVertexShader(vsPtr, (nuint)vertexShaderBytes.Length);
                _pixelShader = device.Device.CreatePixelShader(psPtr, (nuint)pixelShaderBytes.Length);
            }
        }

        _frameConstants = device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = 32,
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
        IReadOnlyList<StrokePath2D> paths,
        float opacityScale,
        NprRenderDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var buildWatch = Stopwatch.StartNew();
        var instances = DxStrokeInstanceBuilder.Build(
            paths,
            opacityScale,
            request.Quality.PreserveLayerOrdering);
        buildWatch.Stop();
        diagnostics.AddTiming("GpuStrokeBuild", buildWatch.Elapsed.TotalMilliseconds, $"instances={instances.Count}");

        if (instances.Count == 0 || target.RenderTargetView is null)
        {
            return;
        }

        unsafe
        {
            using var upload = CreateStructuredBuffer(instances);
            var uploadWatch = Stopwatch.StartNew();
            using var srv = CreateStructuredBufferSrv(upload, instances.Count);

            var constants = stackalloc float[8];
            constants[0] = request.Width;
            constants[1] = request.Height;
            constants[2] = 1f / Math.Max(1, request.Width);
            constants[3] = 1f / Math.Max(1, request.Height);
            constants[4] = MathF.Max(0.25f, request.Quality.GpuStrokeCoverageSoftness);
            _device.Context.UpdateSubresource(_frameConstants, 0, null, (IntPtr)constants, 0, 0);
            uploadWatch.Stop();
            diagnostics.AddTiming("GpuStrokeUpload", uploadWatch.Elapsed.TotalMilliseconds, $"instances={instances.Count}");

            var drawWatch = Stopwatch.StartNew();
            _device.Context.OMSetRenderTargets(target.RenderTargetView);
            _device.Context.RSSetViewport(0, 0, request.Width, request.Height);
            _device.Context.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
            _device.Context.IASetInputLayout(null);
            _device.Context.VSSetShader(_vertexShader);
            _device.Context.PSSetShader(_pixelShader);
            _device.Context.VSSetConstantBuffer(0, _frameConstants);
            _device.Context.VSSetShaderResource(0, srv);
            _device.Context.OMSetBlendState(_states.PremultipliedAlphaBlend);
            _device.Context.RSSetState(_states.NoCullRasterizer);
            _device.Context.OMSetDepthStencilState(_states.DepthDisabled);
            _device.Context.DrawInstanced(6, (uint)instances.Count, 0, 0);
            _device.Context.VSSetShaderResource(0, null!);
            drawWatch.Stop();
            diagnostics.AddTiming("GpuStrokeDraw", drawWatch.Elapsed.TotalMilliseconds, $"instances={instances.Count}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _frameConstants.Dispose();
        _pixelShader.Dispose();
        _vertexShader.Dispose();
        _states.Dispose();
    }

    private unsafe ID3D11Buffer CreateStructuredBuffer(List<DxStrokeInstance> instances)
    {
        var desc = new BufferDescription
        {
            ByteWidth = (uint)(instances.Count * DxStrokeInstance.SizeInBytes),
            BindFlags = BindFlags.ShaderResource,
            Usage = ResourceUsage.Default,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = DxStrokeInstance.SizeInBytes
        };

        var array = CollectionsMarshal.AsSpan(instances);
        fixed (DxStrokeInstance* ptr = array)
        {
            var data = new SubresourceData((IntPtr)ptr, 0, 0);
            return _device.Device.CreateBuffer(desc, data);
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
}
