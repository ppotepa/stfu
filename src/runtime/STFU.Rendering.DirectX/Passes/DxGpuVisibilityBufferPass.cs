using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.DirectX.Device;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxGpuVisibilityBufferPass : IDisposable
{
    private readonly DirectXDevice _device;
    private readonly ID3D11VertexShader _vertexShader;
    private readonly ID3D11PixelShader _pixelShader;
    private readonly ID3D11ComputeShader _reduceShader;
    private readonly ID3D11ComputeShader _edgeSampleShader;
    private readonly ID3D11Buffer _frameConstants;
    private readonly ID3D11Buffer _reduceConstants;
    private readonly ID3D11Buffer _edgeSampleConstants;
    private readonly ID3D11DepthStencilState _depthState;
    private readonly ID3D11RasterizerState _rasterizerState;
    private ID3D11Buffer? _triangleBuffer;
    private ID3D11ShaderResourceView? _triangleSrv;
    private int _triangleCapacity;
    private ID3D11Texture2D? _faceTexture;
    private ID3D11RenderTargetView? _faceRtv;
    private ID3D11ShaderResourceView? _faceSrv;
    private ID3D11Texture2D? _depthTexture;
    private ID3D11DepthStencilView? _depthView;
    private int _resourceWidth;
    private int _resourceHeight;
    private ID3D11Buffer? _visibleBuffer;
    private ID3D11UnorderedAccessView? _visibleUav;
    private ID3D11Buffer? _visibleReadback;
    private int _visibleWordCapacity;
    private ID3D11Buffer? _edgeSampleBuffer;
    private ID3D11ShaderResourceView? _edgeSampleSrv;
    private ID3D11Buffer? _edgeSampleResultBuffer;
    private ID3D11UnorderedAccessView? _edgeSampleResultUav;
    private ID3D11Buffer? _edgeSampleReadback;
    private int _edgeSampleCapacity;
    private VisibilityTriangle[] _stagedTriangles = [];
    private uint[] _visibleWords = [];
    private EdgeSampleRequest[] _edgeSamples = [];
    private uint[] _edgeSampleResults = [];
    private bool _disposed;

    public DxGpuVisibilityBufferPass(DirectXDevice device)
    {
        _device = device;
        var vertexShaderBytes = DirectXShaderCompiler.CompileFromFile("visibility_face_id.hlsl", "VS", "vs_5_0");
        var pixelShaderBytes = DirectXShaderCompiler.CompileFromFile("visibility_face_id.hlsl", "PS", "ps_5_0");
        var reduceShaderBytes = DirectXShaderCompiler.CompileFromFile("visibility_reduce.hlsl", "CS", "cs_5_0");
        var edgeSampleShaderBytes = DirectXShaderCompiler.CompileFromFile("visibility_edge_sample.hlsl", "CS", "cs_5_0");

        unsafe
        {
            fixed (byte* vsPtr = vertexShaderBytes)
            fixed (byte* psPtr = pixelShaderBytes)
            fixed (byte* csPtr = reduceShaderBytes)
            fixed (byte* edgeCsPtr = edgeSampleShaderBytes)
            {
                _vertexShader = device.Device.CreateVertexShader(vsPtr, (nuint)vertexShaderBytes.Length);
                _pixelShader = device.Device.CreatePixelShader(psPtr, (nuint)pixelShaderBytes.Length);
                _reduceShader = device.Device.CreateComputeShader(csPtr, (nuint)reduceShaderBytes.Length);
                _edgeSampleShader = device.Device.CreateComputeShader(edgeCsPtr, (nuint)edgeSampleShaderBytes.Length);
            }
        }

        _frameConstants = CreateConstantBuffer(16);
        _reduceConstants = CreateConstantBuffer(16);
        _edgeSampleConstants = CreateConstantBuffer(16);

        var depthDescription = DepthStencilDescription.Default;
        depthDescription.DepthEnable = true;
        depthDescription.DepthWriteMask = DepthWriteMask.All;
        depthDescription.DepthFunc = ComparisonFunction.LessEqual;
        _depthState = device.Device.CreateDepthStencilState(depthDescription);
        _rasterizerState = device.Device.CreateRasterizerState(RasterizerDescription.CullNone);
    }

    public VisibilityParityStats Execute(
        NprGraph graph,
        int viewportWidth,
        int viewportHeight,
        NprRenderDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var cpuBuffer = graph.DefaultFaceIdVisibility;
        if (!_device.Support.SupportsCompute || cpuBuffer is null || graph.Triangles.Count == 0)
        {
            diagnostics.AddTiming(
                "GpuVisibilityBuffer",
                0,
                $"requested=1, skipped=1, reason={(cpuBuffer is null ? "missingCpuBuffer" : !_device.Support.SupportsCompute ? "computeUnsupported" : "emptyTriangles")}");
            return VisibilityParityStats.Empty;
        }

        var totalWatch = Stopwatch.StartNew();
        var uploadWatch = Stopwatch.StartNew();
        var triangleCount = StageTriangles(graph, cpuBuffer.Width, cpuBuffer.Height, viewportWidth, viewportHeight);
        if (triangleCount == 0)
        {
            uploadWatch.Stop();
            diagnostics.AddTiming(
                "GpuVisibilityBuffer",
                0,
                $"requested=1, skipped=1, reason=noRenderableTriangles, sourceTriangles={graph.Triangles.Count}, sourceVertices={graph.Vertices.Count}, faceCount={cpuBuffer.FaceVisible.Length}");
            return VisibilityParityStats.Empty;
        }

        var wordCount = NumericMath.AtLeast(NumericMath.CeilingDivide(cpuBuffer.FaceVisible.Length, 32), 1);
        var edgeSampleCount = StageEdgeSamples(graph, cpuBuffer.Width, cpuBuffer.Height, viewportWidth, viewportHeight);
        var edgeSampleStats = EdgeSampleStats.Empty;

        var drawWatch = new Stopwatch();
        var reduceWatch = new Stopwatch();
        var edgeSampleWatch = new Stopwatch();
        using (_device.Lock())
        {
            EnsureTriangleCapacity(triangleCount);
            UploadTriangles(triangleCount);
            EnsureVisibilityResources(cpuBuffer.Width, cpuBuffer.Height);
            EnsureVisibleBitsetCapacity(wordCount);
            UpdateConstants(cpuBuffer.Width, cpuBuffer.Height, cpuBuffer.FaceVisible.Length);
            uploadWatch.Stop();

            drawWatch.Start();
            DrawFaceIds((uint)triangleCount);
            drawWatch.Stop();

            edgeSampleWatch.Start();
            edgeSampleStats = RunEdgeSamples(edgeSampleCount, cpuBuffer.Width, cpuBuffer.Height);
            edgeSampleWatch.Stop();

            reduceWatch.Start();
            ReduceVisibleFaces(cpuBuffer.Width, cpuBuffer.Height);
            ReadbackVisibleWords(wordCount);
            reduceWatch.Stop();
        }

        var (matchingFaces, cpuOnlyFaces, gpuOnlyFaces) = CountVisibilityParities(cpuBuffer.FaceVisible, wordCount);
        var gpuVisibleCount = CountGpuVisible(cpuBuffer.FaceVisible.Length, wordCount);
        var cpuVisibleCount = CountCpuVisible(cpuBuffer.FaceVisible);
        var visibility = VisibilityParityStats.FromCounts(
            cpuVisibleCount,
            gpuVisibleCount,
            matchingFaces,
            cpuOnlyFaces,
            gpuOnlyFaces);
        totalWatch.Stop();

        diagnostics.AddTiming(
            "GpuVisibilityBuffer",
            totalWatch.Elapsed.TotalMilliseconds,
            $"requested=1, cpuReferenceFallback=1, triangles={triangleCount}, pixels={cpuBuffer.Width * cpuBuffer.Height}, bitsetBytes={wordCount * 4}, cpuVisible={cpuVisibleCount}, gpuVisible={gpuVisibleCount}, mismatches={visibility.MismatchCount}, edgeSamples={edgeSampleStats.SampleCount}, edgeVisible={edgeSampleStats.VisibleCount}, edgeMismatches={edgeSampleStats.MismatchCount}, upload={uploadWatch.Elapsed.TotalMilliseconds:0.###}, draw={drawWatch.Elapsed.TotalMilliseconds:0.###}, edgeSampleReadback={edgeSampleWatch.Elapsed.TotalMilliseconds:0.###}, reduceReadback={reduceWatch.Elapsed.TotalMilliseconds:0.###}");

        return visibility;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _visibleReadback?.Dispose();
        _visibleUav?.Dispose();
        _visibleBuffer?.Dispose();
        _edgeSampleReadback?.Dispose();
        _edgeSampleResultUav?.Dispose();
        _edgeSampleResultBuffer?.Dispose();
        _edgeSampleSrv?.Dispose();
        _edgeSampleBuffer?.Dispose();
        _depthView?.Dispose();
        _depthTexture?.Dispose();
        _faceSrv?.Dispose();
        _faceRtv?.Dispose();
        _faceTexture?.Dispose();
        _triangleSrv?.Dispose();
        _triangleBuffer?.Dispose();
        _rasterizerState.Dispose();
        _depthState.Dispose();
        _edgeSampleConstants.Dispose();
        _reduceConstants.Dispose();
        _frameConstants.Dispose();
        _edgeSampleShader.Dispose();
        _reduceShader.Dispose();
        _pixelShader.Dispose();
        _vertexShader.Dispose();
    }

    private int StageTriangles(NprGraph graph, int bufferWidth, int bufferHeight, int viewportWidth, int viewportHeight)
    {
        if (_stagedTriangles.Length < graph.Triangles.Count)
        {
            _stagedTriangles = new VisibilityTriangle[graph.Triangles.Count];
        }

        var scaleX = bufferWidth / NumericMath.AtLeast(viewportWidth, 1f);
        var scaleY = bufferHeight / NumericMath.AtLeast(viewportHeight, 1f);
        var vertices = graph.Vertices;
        var write = 0;
        for (var i = 0; i < graph.Triangles.Count; i++)
        {
            var triangle = graph.Triangles[i];
            if ((uint)triangle.A >= (uint)vertices.Count ||
                (uint)triangle.B >= (uint)vertices.Count ||
                (uint)triangle.C >= (uint)vertices.Count)
            {
                continue;
            }

            var a = vertices[triangle.A];
            var b = vertices[triangle.B];
            var c = vertices[triangle.C];
            var faceId = (uint)(i + 1);
            _stagedTriangles[write++] = new VisibilityTriangle(
                new Vector4(a.Position.X * scaleX, a.Position.Y * scaleY, a.Depth01, faceId),
                new Vector4(b.Position.X * scaleX, b.Position.Y * scaleY, b.Depth01, faceId),
                new Vector4(c.Position.X * scaleX, c.Position.Y * scaleY, c.Depth01, faceId));
        }

        return write;
    }

    private int StageEdgeSamples(NprGraph graph, int bufferWidth, int bufferHeight, int viewportWidth, int viewportHeight)
    {
        var fragments = graph.DefaultFragments;
        if (_edgeSamples.Length < fragments.Count)
        {
            _edgeSamples = new EdgeSampleRequest[fragments.Count];
        }

        var scaleX = bufferWidth / NumericMath.AtLeast(viewportWidth, 1f);
        var scaleY = bufferHeight / NumericMath.AtLeast(viewportHeight, 1f);
        var write = 0;
        for (var i = 0; i < fragments.Count; i++)
        {
            var fragment = fragments[i];
            var firstFaceId = ToGpuFaceId(fragment.FirstTriangleIndex, graph.Triangles.Count);
            var secondFaceId = ToGpuFaceId(fragment.SecondTriangleIndex, graph.Triangles.Count);
            if (firstFaceId == 0 && secondFaceId == 0)
            {
                continue;
            }

            var midX = ((fragment.P0.X + fragment.P1.X) * 0.5f) * scaleX;
            var midY = ((fragment.P0.Y + fragment.P1.Y) * 0.5f) * scaleY;
            _edgeSamples[write++] = new EdgeSampleRequest(
                new Vector2(
                    NumericMath.Clamp(NumericMath.Floor(midX), 0f, bufferWidth - 1f),
                    NumericMath.Clamp(NumericMath.Floor(midY), 0f, bufferHeight - 1f)),
                firstFaceId,
                secondFaceId);
        }

        return write;
    }

    private void DrawFaceIds(uint triangleCount)
    {
        var faceRtv = _faceRtv ?? throw new InvalidOperationException("GPU visibility face RTV is not initialized.");
        var depthView = _depthView ?? throw new InvalidOperationException("GPU visibility depth view is not initialized.");
        var triangleSrv = _triangleSrv ?? throw new InvalidOperationException("GPU visibility triangle SRV is not initialized.");

        _device.Context.ClearRenderTargetView(faceRtv, new Color4(0f, 0f, 0f, 0f));
        _device.Context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1f, 0);

        _device.Context.OMSetRenderTargets(1, [faceRtv], depthView);

        _device.Context.RSSetViewport(0, 0, _resourceWidth, _resourceHeight);
        _device.Context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _device.Context.IASetInputLayout(null);
        _device.Context.VSSetShader(_vertexShader);
        _device.Context.PSSetShader(_pixelShader);
        _device.Context.VSSetConstantBuffer(0, _frameConstants);
        _device.Context.VSSetShaderResource(0, triangleSrv);
        _device.Context.OMSetBlendState(null);
        _device.Context.RSSetState(_rasterizerState);
        _device.Context.OMSetDepthStencilState(_depthState);
        _device.Context.Draw(triangleCount * 3, 0);
        _device.Context.VSSetShaderResource(0, null!);
        _device.Context.OMSetRenderTargets(0, [], null);
    }

    private void ReduceVisibleFaces(int width, int height)
    {
        var visibleUav = _visibleUav ?? throw new InvalidOperationException("GPU visibility UAV is not initialized.");
        var faceSrv = _faceSrv ?? throw new InvalidOperationException("GPU visibility face SRV is not initialized.");

        _device.Context.ClearUnorderedAccessView(visibleUav, new Int4(0, 0, 0, 0));
        _device.Context.CSSetShader(_reduceShader);
        _device.Context.CSSetConstantBuffer(0, _reduceConstants);
        _device.Context.CSSetShaderResource(0, faceSrv);
        _device.Context.CSSetUnorderedAccessViews(0, 1, [visibleUav], [0u]);

        _device.Context.Dispatch((uint)((width + 7) / 8), (uint)((height + 7) / 8), 1);
        _device.Context.CSSetShaderResource(0, null!);
        _device.Context.CSSetUnorderedAccessViews(0, 1, [null!], [0u]);
    }

    private EdgeSampleStats RunEdgeSamples(int sampleCount, int width, int height)
    {
        if (sampleCount == 0)
        {
            return EdgeSampleStats.Empty;
        }

        EnsureEdgeSampleCapacity(sampleCount);
        UploadEdgeSamples(sampleCount);
        UpdateEdgeSampleConstants(width, height, sampleCount);

        var sampleSrv = _edgeSampleSrv ?? throw new InvalidOperationException("GPU visibility edge sample SRV is not initialized.");
        var faceSrv = _faceSrv ?? throw new InvalidOperationException("GPU visibility face SRV is not initialized.");
        var resultUav = _edgeSampleResultUav ?? throw new InvalidOperationException("GPU visibility edge sample UAV is not initialized.");

        _device.Context.ClearUnorderedAccessView(resultUav, new Int4(0, 0, 0, 0));
        _device.Context.CSSetShader(_edgeSampleShader);
        _device.Context.CSSetConstantBuffer(0, _edgeSampleConstants);
        _device.Context.CSSetShaderResource(0, sampleSrv);
        _device.Context.CSSetShaderResource(1, faceSrv);
        _device.Context.CSSetUnorderedAccessViews(0, 1, [resultUav], [0u]);
        _device.Context.Dispatch((uint)((sampleCount + 63) / 64), 1, 1);
        _device.Context.CSSetShaderResource(0, null!);
        _device.Context.CSSetShaderResource(1, null!);
        _device.Context.CSSetUnorderedAccessViews(0, 1, [null!], [0u]);

        ReadbackEdgeSampleResults(sampleCount);

        var visible = 0;
        for (var i = 0; i < sampleCount; i++)
        {
            visible += _edgeSampleResults[i] != 0 ? 1 : 0;
        }

        return new EdgeSampleStats(sampleCount, visible, sampleCount - visible);
    }

    private void ReadbackVisibleWords(int wordCount)
    {
        var visibleReadback = _visibleReadback ?? throw new InvalidOperationException("GPU visibility readback buffer is not initialized.");
        var visibleBuffer = _visibleBuffer ?? throw new InvalidOperationException("GPU visibility bitset buffer is not initialized.");

        _device.Context.CopyResource(visibleReadback, visibleBuffer);
        var mapped = _device.Context.Map(visibleReadback, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            if (_visibleWords.Length < wordCount)
            {
                _visibleWords = new uint[wordCount];
            }

            unsafe
            {
                fixed (uint* destination = _visibleWords)
                {
                    Buffer.MemoryCopy(
                        mapped.DataPointer.ToPointer(),
                        destination,
                        (long)_visibleWords.Length * sizeof(uint),
                        (long)wordCount * sizeof(uint));
                }
            }
        }
        finally
        {
            _device.Context.Unmap(visibleReadback, 0);
        }
    }

    private (int Matching, int CpuOnly, int GpuOnly) CountVisibilityParities(bool[] cpuVisible, int wordCount)
    {
        var matching = 0;
        var cpuOnly = 0;
        var gpuOnly = 0;
        if (wordCount <= 0)
        {
            return (0, 0, 0);
        }

        for (var i = 0; i < cpuVisible.Length; i++)
        {
            var gpuVisible = ((_visibleWords[i >> 5] >> (i & 31)) & 1u) != 0;
            if (cpuVisible[i] && gpuVisible)
            {
                matching++;
            }
            else if (cpuVisible[i])
            {
                cpuOnly++;
            }
            else if (gpuVisible)
            {
                gpuOnly++;
            }
        }

        return (matching, cpuOnly, gpuOnly);
    }

    private int CountGpuVisible(int faceCount, int wordCount)
    {
        var count = 0;
        for (var i = 0; i < faceCount; i++)
        {
            if (((_visibleWords[i >> 5] >> (i & 31)) & 1u) != 0)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountCpuVisible(bool[] faceVisible)
    {
        var count = 0;
        for (var i = 0; i < faceVisible.Length; i++)
        {
            if (faceVisible[i])
            {
                count++;
            }
        }

        return count;
    }

    private void EnsureVisibilityResources(int width, int height)
    {
        if (_faceTexture is not null && width == _resourceWidth && height == _resourceHeight)
        {
            return;
        }

        _depthView?.Dispose();
        _depthTexture?.Dispose();
        _faceSrv?.Dispose();
        _faceRtv?.Dispose();
        _faceTexture?.Dispose();

        _resourceWidth = width;
        _resourceHeight = height;

        _faceTexture = _device.Device.CreateTexture2D(new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R32_UInt,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        });
        _faceRtv = _device.Device.CreateRenderTargetView(_faceTexture);
        _faceSrv = _device.Device.CreateShaderResourceView(_faceTexture);

        _depthTexture = _device.Device.CreateTexture2D(new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.D32_Float,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        });
        _depthView = _device.Device.CreateDepthStencilView(_depthTexture);
    }

    private void EnsureTriangleCapacity(int requiredCount)
    {
        if (_triangleBuffer is not null && requiredCount <= _triangleCapacity)
        {
            return;
        }

        _triangleSrv?.Dispose();
        _triangleBuffer?.Dispose();
        _triangleCapacity = NextCapacity(requiredCount);
        _triangleBuffer = _device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = (uint)(_triangleCapacity * VisibilityTriangle.SizeInBytes),
            BindFlags = BindFlags.ShaderResource,
            Usage = ResourceUsage.Dynamic,
            CPUAccessFlags = CpuAccessFlags.Write,
            MiscFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = VisibilityTriangle.SizeInBytes
        });
        _triangleSrv = CreateStructuredBufferSrv(_triangleBuffer, _triangleCapacity, VisibilityTriangle.SizeInBytes);
    }

    private void EnsureVisibleBitsetCapacity(int wordCount)
    {
        if (_visibleBuffer is not null && wordCount <= _visibleWordCapacity)
        {
            return;
        }

        _visibleReadback?.Dispose();
        _visibleUav?.Dispose();
        _visibleBuffer?.Dispose();
        _visibleWordCapacity = NextCapacity(wordCount);
        var byteWidth = _visibleWordCapacity * sizeof(uint);

        _visibleBuffer = _device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = (uint)byteWidth,
            BindFlags = BindFlags.UnorderedAccess,
            Usage = ResourceUsage.Default,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = sizeof(uint)
        });
        _visibleUav = _device.Device.CreateUnorderedAccessView(
            _visibleBuffer,
            new UnorderedAccessViewDescription(
                _visibleBuffer,
                Format.Unknown,
                0,
                (uint)_visibleWordCapacity,
                BufferUnorderedAccessViewFlags.None));

        _visibleReadback = _device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = (uint)byteWidth,
            BindFlags = BindFlags.None,
            Usage = ResourceUsage.Staging,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None,
            StructureByteStride = sizeof(uint)
        });
        if (_visibleWords.Length < _visibleWordCapacity)
        {
            _visibleWords = new uint[_visibleWordCapacity];
        }
    }

    private void EnsureEdgeSampleCapacity(int sampleCount)
    {
        if (_edgeSampleBuffer is not null && sampleCount <= _edgeSampleCapacity)
        {
            return;
        }

        _edgeSampleReadback?.Dispose();
        _edgeSampleResultUav?.Dispose();
        _edgeSampleResultBuffer?.Dispose();
        _edgeSampleSrv?.Dispose();
        _edgeSampleBuffer?.Dispose();
        _edgeSampleCapacity = NextCapacity(sampleCount);

        _edgeSampleBuffer = _device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = (uint)(_edgeSampleCapacity * EdgeSampleRequest.SizeInBytes),
            BindFlags = BindFlags.ShaderResource,
            Usage = ResourceUsage.Dynamic,
            CPUAccessFlags = CpuAccessFlags.Write,
            MiscFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = EdgeSampleRequest.SizeInBytes
        });
        _edgeSampleSrv = CreateStructuredBufferSrv(_edgeSampleBuffer, _edgeSampleCapacity, EdgeSampleRequest.SizeInBytes);

        var resultByteWidth = _edgeSampleCapacity * sizeof(uint);
        _edgeSampleResultBuffer = _device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = (uint)resultByteWidth,
            BindFlags = BindFlags.UnorderedAccess,
            Usage = ResourceUsage.Default,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = sizeof(uint)
        });
        _edgeSampleResultUav = _device.Device.CreateUnorderedAccessView(
            _edgeSampleResultBuffer,
            new UnorderedAccessViewDescription(
                _edgeSampleResultBuffer,
                Format.Unknown,
                0,
                (uint)_edgeSampleCapacity,
                BufferUnorderedAccessViewFlags.None));

        _edgeSampleReadback = _device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = (uint)resultByteWidth,
            BindFlags = BindFlags.None,
            Usage = ResourceUsage.Staging,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None,
            StructureByteStride = sizeof(uint)
        });

        if (_edgeSampleResults.Length < _edgeSampleCapacity)
        {
            _edgeSampleResults = new uint[_edgeSampleCapacity];
        }
    }

    private unsafe void UploadTriangles(int triangleCount)
    {
        var triangleBuffer = _triangleBuffer ?? throw new InvalidOperationException("GPU visibility triangle buffer is not initialized.");
        var mapped = _device.Context.Map(triangleBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        try
        {
            fixed (VisibilityTriangle* source = _stagedTriangles)
            {
                Buffer.MemoryCopy(
                    source,
                    mapped.DataPointer.ToPointer(),
                    (long)_triangleCapacity * VisibilityTriangle.SizeInBytes,
                    (long)triangleCount * VisibilityTriangle.SizeInBytes);
            }
        }
        finally
        {
            _device.Context.Unmap(triangleBuffer, 0);
        }
    }

    private unsafe void UploadEdgeSamples(int sampleCount)
    {
        var edgeSampleBuffer = _edgeSampleBuffer ?? throw new InvalidOperationException("GPU visibility edge sample buffer is not initialized.");
        var mapped = _device.Context.Map(edgeSampleBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        try
        {
            fixed (EdgeSampleRequest* source = _edgeSamples)
            {
                Buffer.MemoryCopy(
                    source,
                    mapped.DataPointer.ToPointer(),
                    (long)_edgeSampleCapacity * EdgeSampleRequest.SizeInBytes,
                    (long)sampleCount * EdgeSampleRequest.SizeInBytes);
            }
        }
        finally
        {
            _device.Context.Unmap(edgeSampleBuffer, 0);
        }
    }

    private unsafe void ReadbackEdgeSampleResults(int sampleCount)
    {
        var edgeSampleReadback = _edgeSampleReadback ?? throw new InvalidOperationException("GPU visibility edge sample readback buffer is not initialized.");
        var edgeSampleResultBuffer = _edgeSampleResultBuffer ?? throw new InvalidOperationException("GPU visibility edge sample result buffer is not initialized.");

        _device.Context.CopyResource(edgeSampleReadback, edgeSampleResultBuffer);
        var mapped = _device.Context.Map(edgeSampleReadback, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            fixed (uint* destination = _edgeSampleResults)
            {
                Buffer.MemoryCopy(
                    mapped.DataPointer.ToPointer(),
                    destination,
                    (long)_edgeSampleResults.Length * sizeof(uint),
                    (long)sampleCount * sizeof(uint));
            }
        }
        finally
        {
            _device.Context.Unmap(edgeSampleReadback, 0);
        }
    }

    private unsafe void UpdateConstants(int width, int height, int faceCount)
    {
        var frame = new VisibilityFrameConstants(
            new Vector4(width, height, NumericMath.InverseAtLeast(width), NumericMath.InverseAtLeast(height)));
        var reduce = new VisibilityReduceConstants((uint)width, (uint)height, (uint)faceCount, 0);
        _device.Context.UpdateSubresource(_frameConstants, 0, null, (IntPtr)(&frame), 0, 0);
        _device.Context.UpdateSubresource(_reduceConstants, 0, null, (IntPtr)(&reduce), 0, 0);
    }

    private unsafe void UpdateEdgeSampleConstants(int width, int height, int sampleCount)
    {
        var constants = new EdgeSampleConstants((uint)width, (uint)height, (uint)sampleCount, 0);
        _device.Context.UpdateSubresource(_edgeSampleConstants, 0, null, (IntPtr)(&constants), 0, 0);
    }

    private ID3D11Buffer CreateConstantBuffer(int byteWidth)
    {
        return _device.Device.CreateBuffer(new BufferDescription
        {
            ByteWidth = (uint)byteWidth,
            BindFlags = BindFlags.ConstantBuffer,
            Usage = ResourceUsage.Default,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None,
            StructureByteStride = 0
        });
    }

    private ID3D11ShaderResourceView CreateStructuredBufferSrv(ID3D11Buffer buffer, int elementCount, int elementSize)
    {
        var desc = new ShaderResourceViewDescription(
            buffer,
            Format.Unknown,
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

    private static uint ToGpuFaceId(int triangleIndex, int triangleCount)
    {
        return (uint)triangleIndex < (uint)triangleCount
            ? (uint)(triangleIndex + 1)
            : 0u;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct VisibilityTriangle(Vector4 A, Vector4 B, Vector4 C)
    {
        public const int SizeInBytes = 48;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct VisibilityFrameConstants(Vector4 BufferSizeInv);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct VisibilityReduceConstants(uint Width, uint Height, uint FaceCount, uint Padding0);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct EdgeSampleRequest(Vector2 Pixel, uint FirstFaceId, uint SecondFaceId)
    {
        public const int SizeInBytes = 16;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct EdgeSampleConstants(uint Width, uint Height, uint SampleCount, uint Padding0);

    private readonly record struct EdgeSampleStats(int SampleCount, int VisibleCount, int MismatchCount)
    {
        public static EdgeSampleStats Empty { get; } = new(0, 0, 0);
    }
}
