using System.Diagnostics;
using System.Runtime.InteropServices;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Common.Math;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Diagnostics;
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
    private readonly List<DxStrokeInstance> _instances = [];
    private readonly List<DxStrokeInstanceBuilder.PathSortEntry> _sortScratch = [];
    private ID3D11Buffer? _instanceBuffer;
    private ID3D11ShaderResourceView? _instanceBufferSrv;
    private int _instanceCapacity;
    private int _lastSegmentCount;
    private int _lastSegmentHash;
    private int _lastLayerOpacityHash;
    private bool _instanceCacheValid;
    private bool _disposed;

    public DirectXRenderCounters Counters { get; } = new();

    public DxStrokeRasterPass(DirectXDevice device)
    {
        _device = device;
        _states = new DirectXPipelineStates(device.Device);

        var vertexShaderBytes = DirectXShaderCompiler.CompileFromFile("stroke_raster.hlsl", "VS", "vs_5_0");
        var pixelShaderBytes = DirectXShaderCompiler.CompileFromFile("stroke_raster.hlsl", "PS", "ps_5_0");

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
            _instances,
            _sortScratch,
            request.Quality.PreserveLayerOrdering);
        buildWatch.Stop();
        Counters.StrokeInstancesBuilt += instances.Count;
        diagnostics.AddTiming("GpuStrokeBuild", buildWatch.Elapsed.TotalMilliseconds, $"instances={instances.Count}, source=paths");

        RenderInstances(target, request, instances, diagnostics);
    }

    public void Execute(
        DirectXTextureResource target,
        NprRenderRequest request,
        IReadOnlyList<StrokeSegment2D> segments,
        float opacityScale,
        NprRenderDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var buildWatch = Stopwatch.StartNew();
        var segmentHash = ComputeSegmentHash(segments);
        var opacityHash = opacityScale.GetHashCode();
        if (!_instanceCacheValid ||
            _lastSegmentCount != segments.Count ||
            _lastSegmentHash != segmentHash ||
            _lastLayerOpacityHash != opacityHash)
        {
            DxStrokeInstanceBuilder.Build(
                segments,
                opacityScale,
                _instances);
            _lastSegmentCount = segments.Count;
            _lastSegmentHash = segmentHash;
            _lastLayerOpacityHash = opacityHash;
            _instanceCacheValid = true;
            diagnostics.Counters["DxStrokeRasterPass.instanceCacheMiss"] = diagnostics.Counters.TryGetValue("DxStrokeRasterPass.instanceCacheMiss", out var miss)
                ? miss + 1
                : 1;
        }
        else
        {
            diagnostics.Counters["DxStrokeRasterPass.instanceCacheHit"] = diagnostics.Counters.TryGetValue("DxStrokeRasterPass.instanceCacheHit", out var hit)
                ? hit + 1
                : 1;
        }

        var instances = _instances;
        buildWatch.Stop();
        Counters.StrokeInstancesBuilt += instances.Count;
        diagnostics.AddTiming("GpuStrokeBuild", buildWatch.Elapsed.TotalMilliseconds, $"instances={instances.Count}, source=segments");

        RenderInstances(target, request, instances, diagnostics);
    }

    private void RenderInstances(
        DirectXTextureResource target,
        NprRenderRequest request,
        List<DxStrokeInstance> instances,
        NprRenderDiagnostics diagnostics)
    {
        if (instances.Count == 0 || target.RenderTargetView is null)
        {
            return;
        }

        using (_device.Lock())
        {
            unsafe
            {
                var uploadWatch = Stopwatch.StartNew();
                var bufferRecreated = EnsureInstanceBufferCapacity(instances.Count);
                Counters.StrokeInstanceCapacity = _instanceCapacity;
                UploadInstances(instances);
                var uploadedBytes = instances.Count * DxStrokeInstance.SizeInBytes;
                Counters.StrokeInstanceUploads++;
                Counters.UploadedBytes += uploadedBytes;
                if (bufferRecreated)
                {
                    Counters.StrokeInstanceBufferRecreates++;
                }

                var constants = stackalloc float[8];
                constants[0] = request.Width;
                constants[1] = request.Height;
                constants[2] = NumericMath.InverseAtLeast(request.Width);
                constants[3] = NumericMath.InverseAtLeast(request.Height);
                constants[4] = NumericMath.AtLeast(request.Quality.GpuStrokeCoverageSoftness, 0.25f);
                _device.Context.UpdateSubresource(_frameConstants, 0, null, (IntPtr)constants, 0, 0);
                uploadWatch.Stop();
                diagnostics.AddTiming("GpuStrokeUpload", uploadWatch.Elapsed.TotalMilliseconds, $"instances={instances.Count}, bytes={uploadedBytes}, recreated={(bufferRecreated ? 1 : 0)}, capacity={_instanceCapacity}, reuseRatio={Counters.StrokeInstanceUploadReuseRatio:0.000}");
            }

            using (new DirectXGpuTimer(_device, request.Budget.EnableGpuTiming).Measure(
                       (milliseconds, mode) => diagnostics.AddTiming(
                           "GpuStrokeDraw",
                           milliseconds,
                           $"instances={instances.Count}, mode={mode}")))
            {
                _device.Context.OMSetRenderTargets(target.RenderTargetView);
                _device.Context.RSSetViewport(0, 0, request.Width, request.Height);
                _device.Context.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
                _device.Context.IASetInputLayout(null);
                _device.Context.VSSetShader(_vertexShader);
                _device.Context.PSSetShader(_pixelShader);
                _device.Context.VSSetConstantBuffer(0, _frameConstants);
                _device.Context.VSSetShaderResource(0, _instanceBufferSrv);
                _device.Context.OMSetBlendState(_states.PremultipliedAlphaBlend);
                _device.Context.RSSetState(_states.NoCullRasterizer);
                _device.Context.OMSetDepthStencilState(_states.DepthDisabled);
                _device.Context.DrawInstanced(6, (uint)instances.Count, 0, 0);
                _device.Context.VSSetShaderResource(0, null!);
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
        _instanceBufferSrv?.Dispose();
        _instanceBuffer?.Dispose();
        _frameConstants.Dispose();
        _pixelShader.Dispose();
        _vertexShader.Dispose();
        _states.Dispose();
    }

    private bool EnsureInstanceBufferCapacity(int instanceCount)
    {
        if (_instanceBuffer is not null && instanceCount <= _instanceCapacity)
        {
            return false;
        }

        _instanceBufferSrv?.Dispose();
        _instanceBuffer?.Dispose();

        _instanceCapacity = NextCapacity(instanceCount);
        var desc = new BufferDescription
        {
            ByteWidth = (uint)(_instanceCapacity * DxStrokeInstance.SizeInBytes),
            BindFlags = BindFlags.ShaderResource,
            Usage = ResourceUsage.Dynamic,
            CPUAccessFlags = CpuAccessFlags.Write,
            MiscFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = DxStrokeInstance.SizeInBytes
        };

        _instanceBuffer = _device.Device.CreateBuffer(desc);
        _instanceBufferSrv = CreateStructuredBufferSrv(_instanceBuffer, _instanceCapacity);
        return true;
    }

    private unsafe void UploadInstances(List<DxStrokeInstance> instances)
    {
        if (_instanceBuffer is null)
        {
            throw new InvalidOperationException("Stroke instance buffer is not initialized.");
        }

        var mapped = _device.Context.Map(_instanceBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
        try
        {
            var span = CollectionsMarshal.AsSpan(instances);
            fixed (DxStrokeInstance* source = span)
            {
                Buffer.MemoryCopy(
                    source,
                    mapped.DataPointer.ToPointer(),
                    (long)_instanceCapacity * DxStrokeInstance.SizeInBytes,
                    (long)instances.Count * DxStrokeInstance.SizeInBytes);
            }
        }
        finally
        {
            _device.Context.Unmap(_instanceBuffer, 0);
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
            capacity = checked(capacity + Math.Max(capacity >> 1, 1));
        }

        return capacity;
    }

    private static int ComputeSegmentHash(IReadOnlyList<StrokeSegment2D> segments)
    {
        var hash = new HashCode();
        hash.Add(segments.Count);
        var step = Math.Max(1, segments.Count / 64);
        for (var i = 0; i < segments.Count; i += step)
        {
            var segment = segments[i];
            hash.Add(segment.Start.X);
            hash.Add(segment.Start.Y);
            hash.Add(segment.End.X);
            hash.Add(segment.End.Y);
            hash.Add(segment.Style.Thickness);
            hash.Add(segment.Style.Color);
        }

        return hash.ToHashCode();
    }
}
