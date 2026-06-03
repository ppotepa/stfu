using STFU.Engine.Composition;
using STFU.Engine.Scenes;
using STFU.Messaging.Commands;

namespace STFU.Engine;

public sealed class StfuEngine
{
    internal StfuEngine(
        EngineRegistry registry,
        CommandDispatcher commands,
        Scene scene)
    {
        Registry = registry;
        Commands = commands;
        Scene = scene;
    }

    public EngineRegistry Registry { get; }

    public CommandDispatcher Commands { get; }

    public Scene Scene { get; }

    public static StfuEngine Create()
    {
        return StfuEngineBuilder.Create().Build();
    }

    public int Tick(CommandBuffer commandBuffer)
    {
        return Commands.DispatchAll(commandBuffer);
    }
}
