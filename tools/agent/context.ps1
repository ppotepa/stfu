[CmdletBinding()]
param(
    [int]$MaxFiles,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$AgentRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Forward = @()
if ($MaxFiles -gt 0) { $Forward += @("--max", $MaxFiles) }
$Forward += $Args
& (Join-Path $AgentRoot "agent.ps1") context @Forward
exit $LASTEXITCODE
