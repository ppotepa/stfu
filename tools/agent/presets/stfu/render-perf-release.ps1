[CmdletBinding()]
param()

$AgentRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
& (Join-Path $AgentRoot "agent.ps1") build --configuration Release --format json
& (Join-Path $AgentRoot "agent.ps1") bench-history --threshold 5 --format table
& (Join-Path $AgentRoot "agent.ps1") hotspots --scope render --max 30 --format table
exit $LASTEXITCODE
