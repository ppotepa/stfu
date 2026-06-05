[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Solution,

    [switch]$NoRestore,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$AgentRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Forward = @()
if ($Solution) { $Forward += @("--solution", $Solution) }
if ($NoRestore) { $Forward += "--no-restore" }
$Forward += $Args
& (Join-Path $AgentRoot "agent.ps1") build @Forward
exit $LASTEXITCODE
