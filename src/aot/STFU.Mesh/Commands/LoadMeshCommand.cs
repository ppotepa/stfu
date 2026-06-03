using STFU.Messaging.Commands;

namespace STFU.Mesh.Commands;

public readonly record struct LoadMeshCommand(string Path) : ICommand;
