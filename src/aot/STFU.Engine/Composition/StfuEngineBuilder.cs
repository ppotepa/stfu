using STFU.Abstractions.Modules;

namespace STFU.Engine.Composition;

public sealed class StfuEngineBuilder
{
    private readonly EngineRegistry _registry = new();
    private readonly Messaging.Commands.CommandDispatcher _commands = new();
    private readonly Scenes.Scene _scene = new();

    private StfuEngineBuilder()
    {
        _registry.AddSingleton(_scene);
    }

    public static StfuEngineBuilder Create()
    {
        return new StfuEngineBuilder()
            .AddModule(new EngineCoreModule());
    }

    public StfuEngineBuilder AddModule(IEngineModule module)
    {
        module.Register(new EngineModuleContext(_registry, _commands));
        return this;
    }

    public StfuEngineBuilder AddSingleton<TService>(TService service)
        where TService : notnull
    {
        _registry.AddSingleton(service);
        return this;
    }

    public StfuEngineBuilder Configure(Action<EngineRegistry> configure)
    {
        configure(_registry);
        return this;
    }

    public StfuEngine Build()
    {
        return new StfuEngine(_registry, _commands, _scene);
    }
}
