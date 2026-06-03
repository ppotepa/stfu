# STFU

Scene-To-Flat Unrenderer.

STFU is a minimal .NET 10 boilerplate split between AOT-safe code and runtime applications.

## Projects

- `src/aot/STFU.Abstractions`: AOT-friendly shared contracts.
- `src/aot/STFU.Engine`: AOT-friendly builder and registry.
- `src/aot/STFU.Common`: AOT-friendly shared primitives and math types.
- `src/aot/STFU.Mesh`: AOT-friendly mesh model and mesh factory.
- `src/aot/STFU.Mesh.IO`: AOT-friendly mesh loader implementations.
- `src/aot/STFU.Messaging`: AOT-friendly command, event, and snapshot primitives.
- `src/aot/STFU.Projection`: AOT-friendly camera and projection state.
- `src/aot/STFU.Strokes`: AOT-friendly 2D stroke frame model.
- `src/aot/STFU.Viewport`: AOT-friendly viewport state and snapshots.
- `src/runtime/STFU.App`: console runtime host.
- `src/runtime/STFU.UI`: Avalonia desktop shell.

## Composition

```csharp
var engine = StfuEngineBuilder
    .Create()
    .AddModule(new AssetsModule())
    .AddModule(new MeshModule())
    .AddModule(new MeshIOModule())
    .AddModule(new ProjectionModule())
    .AddModule(new StrokesModule())
    .AddModule(new ViewportModule())
    .Build();
```

`STFU.Engine` uses an explicit builder and registry instead of runtime assembly scanning.

## Command Flow

Runtime code sends commands into a buffer. The engine consumes them during `Tick`.

```csharp
var commands = new CommandBuffer();

commands.Enqueue(new CreateEntityCommand("Suzanne"));
commands.Enqueue(new AssignMeshToEntityCommand(entityId, meshHandle));

engine.Tick(commands);
```

Modules register services and command handlers explicitly:

```csharp
public sealed class MeshModule : IEngineModule
{
    public void Register(EngineModuleContext context)
    {
        context.Services.AddSingleton(new MeshFactory());
        context.Commands.Register(new AssignMeshToEntityCommandHandler(context.Scene));
    }
}
```

Current module flow targets the viewport first. Export modules can be added later when file output is needed.

## Build

```powershell
dotnet build STFU.slnx
```

## Run

```powershell
dotnet run --project src/runtime/STFU.App
dotnet run --project src/runtime/STFU.UI
```
