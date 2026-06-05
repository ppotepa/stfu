[CmdletBinding()]
param(
    [string[]]$Path = @(),

    [int]$MaxLines,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$AgentRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Forward = @()
if ($MaxLines -gt 0) { $Forward += @("--max", $MaxLines) }
$Forward += $Args
& (Join-Path $AgentRoot "agent.ps1") diff @Forward
exit $LASTEXITCODE
