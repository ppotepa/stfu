[CmdletBinding()]
param([string]$Since = "HEAD~1")

$AgentRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
& (Join-Path $AgentRoot "agent.ps1") diff --semantic --scope render --format json |
    & (Join-Path $AgentRoot "agent.ps1") hotspots --from-stdin --max 30 --format json |
    & (Join-Path $AgentRoot "agent.ps1") concat --from-stdin --budget-tokens 10000 --format markdown
exit $LASTEXITCODE
