using STFU.Abstractions.Modules;
using STFU.Messaging.Commands;

namespace STFU.Engine.Composition;

public sealed class EngineModuleContext : IModuleContext
{
    public EngineModuleContext(
        EngineRegistry services,
        CommandDispatcher commands)
    {
        Services = services;
        Commands = commands;
    }

    public IModuleServiceRegistry Services { get; }

    public ICommandRegistry Commands { get; }
}
