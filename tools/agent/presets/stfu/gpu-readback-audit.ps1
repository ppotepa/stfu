[CmdletBinding()]
param([switch]$IncludeNative)

$AgentRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
& (Join-Path $AgentRoot "agent.ps1") search --pattern "Map|CopyResource|Readback|GetData|Wait|Result" --path src --format json |
    & (Join-Path $AgentRoot "agent.ps1") member --from-stdin --context 8 --max 25 --format markdown
exit $LASTEXITCODE
