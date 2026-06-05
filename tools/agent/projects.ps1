[CmdletBinding()]
param(
    [int]$MaxProjects,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$AgentRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Forward = @()
if ($MaxProjects -gt 0) { $Forward += @("--max", $MaxProjects) }
$Forward += $Args
& (Join-Path $AgentRoot "agent.ps1") projects @Forward
exit $LASTEXITCODE
