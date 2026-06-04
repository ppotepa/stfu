using STFU.Abstractions.Modules;

namespace STFU.Engine.Composition;

public sealed class EngineRegistry : IModuleServiceRegistry
{
    private readonly Dictionary<Type, object> _services = new();

    public int Count => _services.Count;

    public IModuleServiceRegistry AddSingleton<TService>(TService service)
        where TService : notnull
    {
        _services[typeof(TService)] = service;
        return this;
    }

    public bool Contains<TService>()
        where TService : notnull
    {
        return _services.ContainsKey(typeof(TService));
    }

    public bool TryGet<TService>(out TService service)
        where TService : notnull
    {
        if (_services.TryGetValue(typeof(TService), out var value) && value is TService typed)
        {
            service = typed;
            return true;
        }

        service = default!;
        return false;
    }

    public TService GetRequired<TService>()
        where TService : notnull
    {
        if (TryGet<TService>(out var service))
        {
            return service;
        }

        throw new InvalidOperationException($"Engine service is not registered: {typeof(TService).FullName}");
    }
}
