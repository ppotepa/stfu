[CmdletBinding()]
param()

$AgentRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
& (Join-Path $AgentRoot "agent.ps1") hotspots --rules alloc-in-loop,linq-in-loop,closure-in-loop,boxing,string-concat-loop,large-array-allocation --max 40 --format json |
    & (Join-Path $AgentRoot "agent.ps1") member --from-stdin --context 8 --budget-lines 600 --format markdown
exit $LASTEXITCODE
