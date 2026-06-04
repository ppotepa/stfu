namespace STFU.Abstractions.Modules;

public interface IModuleServiceRegistry
{
    IModuleServiceRegistry AddSingleton<TService>(TService service)
        where TService : notnull;

    bool Contains<TService>()
        where TService : notnull;

    bool TryGet<TService>(out TService service)
        where TService : notnull;

    TService GetRequired<TService>()
        where TService : notnull;
}
