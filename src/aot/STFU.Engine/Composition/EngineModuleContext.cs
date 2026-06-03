using STFU.Messaging.Commands;

namespace STFU.Engine.Composition;

public sealed class EngineModuleContext
{
    public EngineModuleContext(
        EngineRegistry services,
        CommandDispatcher commands,
        Scenes.Scene scene)
    {
        Services = services;
        Commands = commands;
        Scene = scene;
    }

    public EngineRegistry Services { get; }

    public CommandDispatcher Commands { get; }

    public Scenes.Scene Scene { get; }
}
