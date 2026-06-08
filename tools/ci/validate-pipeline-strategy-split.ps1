$ErrorActionPreference = 'Stop'

dotnet restore STFU.slnx
dotnet build STFU.slnx -c Debug --no-restore
dotnet build STFU.slnx -c Release --no-restore
dotnet test -c Release --no-build
