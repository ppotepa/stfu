using System.Diagnostics;
using STFU.Common.Math;
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
    private readonly List<DxToneSurfaceUpload> _uploads = [];
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
        var cacheHits = 0;
        var cacheMisses = 0;
        var uploadWatch = Stopwatch.StartNew();
        _uploads.Clear();
        _uploads.EnsureCapacity(layer.Tones.Count);
        foreach (var tone in layer.Tones)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var upload = _uploader.Upload(tone, out var cacheHit);
            if (upload is null)
            {
                continue;
            }

            _uploads.Add(upload);
            uploaded++;
            if (cacheHit)
            {
                cacheHits++;
            }
            else
            {
                cacheMisses++;
            }
        }

        uploadWatch.Stop();
        diagnostics.AddTiming(
            "GpuToneSurfaceUpload",
            uploadWatch.Elapsed.TotalMilliseconds,
            $"tones={uploaded}; cacheHits={cacheHits}; cacheMisses={cacheMisses}");

        if (_uploads.Count == 0 || target.RenderTargetView is null)
        {
            for (var i = 0; i < _uploads.Count; i++)
            {
                _uploads[i].Dispose();
            }

            _uploads.Clear();
            return;
        }

        try
        {
            using (_device.Lock())
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
                    for (var i = 0; i < _uploads.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var upload = _uploads[i];
                        constants[0] = request.Width;
                        constants[1] = request.Height;
                        constants[2] = NumericMath.InverseAtLeast(request.Width);
                        constants[3] = NumericMath.InverseAtLeast(request.Height);
                        constants[4] = NumericMath.Clamp01(upload.Source.Opacity * layer.Opacity);
                        _device.Context.UpdateSubresource(_frameConstants, 0, null, (IntPtr)constants, 0, 0);
                        _device.Context.PSSetShaderResource(0, upload.ShaderResourceView);
                        _device.Context.Draw(3, 0);
                    }
                }

                _device.Context.PSSetShaderResource(0, null!);
                drawWatch.Stop();
                diagnostics.AddTiming("GpuToneSurfaceDraw", drawWatch.Elapsed.TotalMilliseconds, $"tones={_uploads.Count}");
            }
        }
        finally
        {
            for (var i = 0; i < _uploads.Count; i++)
            {
                _uploads[i].Dispose();
            }

            _uploads.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _uploader.Dispose();
        _sampler.Dispose();
        _frameConstants.Dispose();
        _pixelShader.Dispose();
        _vertexShader.Dispose();
        _states.Dispose();
    }
}
