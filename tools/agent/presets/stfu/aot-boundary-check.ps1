[CmdletBinding()]
param()

$AgentRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
& (Join-Path $AgentRoot "agent.ps1") hotspots --rules reflection,dynamic,pinvoke,unsafe --max 40 --format json |
    & (Join-Path $AgentRoot "agent.ps1") concat --from-stdin --budget-tokens 8000 --format markdown
& (Join-Path $AgentRoot "agent.ps1") packages --native-assets --format table
exit $LASTEXITCODE
