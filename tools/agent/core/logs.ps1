[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Args)
$AgentRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
& (Join-Path $AgentRoot "agent.ps1") logs @Args
exit $LASTEXITCODE
