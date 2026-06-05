[CmdletBinding()]
param(
    [string]$Since = "HEAD~1",
    [switch]$Run
)

$AgentRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
& (Join-Path $AgentRoot "agent.ps1") bench-history --benchmark fbx-mesh-render --regressions-only --format json |
    & (Join-Path $AgentRoot "agent.ps1") diff --from-stdin --semantic --format json |
    & (Join-Path $AgentRoot "agent.ps1") hotspots --from-stdin --max 25 --format json |
    & (Join-Path $AgentRoot "agent.ps1") concat --from-stdin --budget-tokens 9000 --format markdown
exit $LASTEXITCODE
