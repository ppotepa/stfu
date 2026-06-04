using Vortice.Direct3D11;
using Vortice.DXGI;

namespace STFU.Rendering.DirectX.Device;

public sealed class DirectXDevice : IDisposable
{
    private readonly object _gate = new();
    private bool _disposed;

    public DirectXDevice(
        IDXGIFactory4 factory,
        IDXGIAdapter1 adapter,
        ID3D11Device device,
        ID3D11DeviceContext context,
        DirectXFeatureSupport support)
    {
        Factory = factory;
        Adapter = adapter;
        Device = device;
        Context = context;
        Support = support;
        Resources = new DirectXResourceRegistry();
        TexturePool = new DirectXTexturePool(this);
    }

    public IDXGIFactory4 Factory { get; }

    public IDXGIAdapter1 Adapter { get; }

    public ID3D11Device Device { get; }

    public ID3D11DeviceContext Context { get; }

    public DirectXFeatureSupport Support { get; }

    public DirectXResourceRegistry Resources { get; }

    public DirectXTexturePool TexturePool { get; }

    public bool IsDisposed => _disposed;

    public IDisposable Lock()
    {
        Monitor.Enter(_gate);
        return new Releaser(_gate);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        TexturePool.Dispose();
        Resources.Dispose();
        Context.ClearState();
        Context.Flush();
        Context.Dispose();
        Device.Dispose();
        Adapter.Dispose();
        Factory.Dispose();
    }

    private sealed class Releaser : IDisposable
    {
        private object? _gate;

        public Releaser(object gate)
        {
            _gate = gate;
        }

        public void Dispose()
        {
            if (_gate is { } gate)
            {
                _gate = null;
                Monitor.Exit(gate);
            }
        }
    }
}
