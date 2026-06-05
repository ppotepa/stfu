[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Pattern,

    [string[]]$Path = @(),

    [int]$MaxLines,

    [switch]$Literal,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$AgentRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Forward = @()
if ($Pattern) { $Forward += @("--pattern", $Pattern) }
if ($Path.Count -gt 0) { $Forward += @("--path", $Path[0]) }
if ($MaxLines -gt 0) { $Forward += @("--max", $MaxLines) }
if ($Literal) { $Forward += "--literal" }
$Forward += $Args
& (Join-Path $AgentRoot "agent.ps1") search @Forward
exit $LASTEXITCODE
