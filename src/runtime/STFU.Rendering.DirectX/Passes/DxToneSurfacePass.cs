using System.Diagnostics;
using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Device;
using STFU.Rendering.DirectX.Upload;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxToneSurfacePass : IDisposable
{
    private readonly DirectXDevice _device;
    private readonly DirectXPipelineStates _states;
    private readonly DxToneSurfaceUploader _uploader;
    private readonly ID3D11VertexShader _vertexShader;
    private readonly ID3D11PixelShader _pixelShader;
    private readonly ID3D11Buffer _frameConstants;
    private readonly ID3D11SamplerState _sampler;
    private bool _disposed;

    public DxToneSurfacePass(DirectXDevice device)
    {
        _device = device;
        _states = new DirectXPipelineStates(device.Device);
        _uploader = new DxToneSurfaceUploader(device);

        var vertexShaderBytes = DirectXShaderCompiler.CompileFromFile("tone_surface.hlsl", "VS", "vs_5_0");
        var pixelShaderBytes = DirectXShaderCompiler.CompileFromFile("tone_surface.hlsl", "PS", "ps_5_0");

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

        _sampler = device.Device.CreateSamplerState(new SamplerDescription(
            Filter.MinMagMipLinear,
            TextureAddressMode.Clamp,
            TextureAddressMode.Clamp,
            TextureAddressMode.Clamp,
            0f,
            1,
            ComparisonFunction.Never,
            new Color4(0f, 0f, 0f, 0f),
            0f,
            float.MaxValue));
    }

    public void Execute(
        DirectXTextureResource target,
        NprRenderRequest request,
        NprLayerFrame layer,
        NprRenderDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var uploaded = 0;
        var uploadWatch = Stopwatch.StartNew();
        var uploads = new List<DxToneSurfaceUpload>(layer.Tones.Count);
        foreach (var tone in layer.Tones)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var upload = _uploader.Upload(tone);
            if (upload is null)
            {
                continue;
            }

            uploads.Add(upload);
            uploaded++;
        }

        uploadWatch.Stop();
        diagnostics.AddTiming("GpuToneSurfaceUpload", uploadWatch.Elapsed.TotalMilliseconds, $"tones={uploaded}");

        if (uploads.Count == 0 || target.RenderTargetView is null)
        {
            foreach (var upload in uploads)
            {
                upload.Dispose();
            }

            return;
        }

        try
        {
            var drawWatch = Stopwatch.StartNew();
            _device.Context.OMSetRenderTargets(target.RenderTargetView);
            _device.Context.RSSetViewport(0, 0, request.Width, request.Height);
            _device.Context.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
            _device.Context.IASetInputLayout(null);
            _device.Context.VSSetShader(_vertexShader);
            _device.Context.PSSetShader(_pixelShader);
            _device.Context.PSSetSampler(0, _sampler);
            _device.Context.VSSetConstantBuffer(0, _frameConstants);
            _device.Context.PSSetConstantBuffer(0, _frameConstants);
            _device.Context.OMSetBlendState(_states.PremultipliedAlphaBlend);
            _device.Context.RSSetState(_states.NoCullRasterizer);
            _device.Context.OMSetDepthStencilState(_states.DepthDisabled);

            unsafe
            {
                var constants = stackalloc float[8];
                foreach (var upload in uploads)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    constants[0] = request.Width;
                    constants[1] = request.Height;
                    constants[2] = 1f / Math.Max(1, request.Width);
                    constants[3] = 1f / Math.Max(1, request.Height);
                    constants[4] = Math.Clamp(upload.Source.Opacity * layer.Opacity, 0f, 1f);
                    _device.Context.UpdateSubresource(_frameConstants, 0, null, (IntPtr)constants, 0, 0);
                    _device.Context.PSSetShaderResource(0, upload.ShaderResourceView);
                    _device.Context.Draw(3, 0);
                }
            }

            _device.Context.PSSetShaderResource(0, null!);
            drawWatch.Stop();
            diagnostics.AddTiming("GpuToneSurfaceDraw", drawWatch.Elapsed.TotalMilliseconds, $"tones={uploads.Count}");
        }
        finally
        {
            foreach (var upload in uploads)
            {
                upload.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _sampler.Dispose();
        _frameConstants.Dispose();
        _pixelShader.Dispose();
        _vertexShader.Dispose();
        _states.Dispose();
    }
}
