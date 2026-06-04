[CmdletBinding()]
param(
    [string]$Solution = 'STFU.slnx',
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'

$args = @('build', $Solution, '-v', 'minimal')
if ($NoRestore) {
    $args += '--no-restore'
}

dotnet @args
