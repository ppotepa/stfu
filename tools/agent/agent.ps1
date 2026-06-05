[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Command = "help",

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args,

    [Parameter(ValueFromPipeline = $true)]
    [object]$InputObject
)

begin {
    $ErrorActionPreference = "Stop"
    $PipelineBuffer = New-Object System.Collections.Generic.List[string]

    function Find-RepoRoot {
        param([string]$Start)

        $current = Resolve-Path $Start
        while ($current) {
            if ((Test-Path (Join-Path $current ".git")) -or
                (Test-Path (Join-Path $current "global.json")) -or
                (Get-ChildItem -Path $current -Filter "*.sln*" -File -ErrorAction SilentlyContinue | Select-Object -First 1)) {
                return $current.ToString()
            }

            $parent = Split-Path -Parent $current
            if (!$parent -or $parent -eq $current) {
                break
            }

            $current = $parent
        }

        throw "Could not find repository root."
    }

    function Test-AgentCliStale {
        param(
            [string]$ProjectPath,
            [string]$OutputPath
        )

        if (!(Test-Path $OutputPath)) {
            return $true
        }

        $outputTime = (Get-Item $OutputPath).LastWriteTimeUtc
        $newestSource = Get-ChildItem -Path (Split-Path -Parent $ProjectPath) -Recurse -File -Include *.cs,*.csproj |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1

        return $newestSource -and $newestSource.LastWriteTimeUtc -gt $outputTime
    }
}

process {
    if ($null -ne $InputObject) {
        $PipelineBuffer.Add([string]$InputObject)
    }
}

end {
    $AgentRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $RepoRoot = Find-RepoRoot $AgentRoot
    $Project = Join-Path $AgentRoot "src\Agent.Cli\Agent.Cli.csproj"
    $Cli = Join-Path $AgentRoot "bin\Agent.Cli.dll"

    if (Test-AgentCliStale $Project $Cli) {
        $mutex = New-Object System.Threading.Mutex($false, "STFU.AgentCli.Build")
        try {
            [void]$mutex.WaitOne()
            if (Test-AgentCliStale $Project $Cli) {
                $buildOutput = dotnet build $Project -v minimal -o (Join-Path $AgentRoot "bin") 2>&1
                foreach ($line in $buildOutput) {
                    [Console]::Error.WriteLine($line)
                }
                if ($LASTEXITCODE -ne 0) {
                    exit $LASTEXITCODE
                }
            }
        }
        finally {
            $mutex.ReleaseMutex()
            $mutex.Dispose()
        }
    }

    function Test-AgentServerEligible {
        param([string]$Name)
        return @("symbols", "refs", "member", "graph", "projects", "search") -contains $Name.ToLowerInvariant()
    }

    if (Test-AgentServerEligible $Command) {
        $cacheDir = Join-Path $RepoRoot ".agents\cache"
        $serverState = Join-Path $cacheDir "server.json"
        if (!(Test-Path $serverState)) {
            New-Item -ItemType Directory -Force -Path $cacheDir | Out-Null
            Start-Process -FilePath "dotnet" `
                -ArgumentList @($Cli, "server-daemon") `
                -WorkingDirectory $RepoRoot `
                -WindowStyle Hidden | Out-Null
        }
    }

    Push-Location $RepoRoot
    try {
        if ($PipelineBuffer.Count -gt 0) {
            $PipelineBuffer | dotnet $Cli $Command @Args
        }
        else {
            dotnet $Cli $Command @Args
        }
        exit $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
}
