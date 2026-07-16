# Module purpose: Scans the current non-ignored worktree and available Git history for high-confidence secret signatures without printing values.
[CmdletBinding()]
param(
    [switch]$SkipHistory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$patterns = @(
    [pscustomobject]@{ Name = "PrivateKey"; Git = "-----BEGIN ([A-Z0-9 ]+ )?PRIVATE KEY-----"; DotNet = "-----BEGIN ([A-Z0-9 ]+ )?PRIVATE KEY-----"; IgnoreCase = $false },
    [pscustomobject]@{ Name = "AwsAccessKey"; Git = "AKIA[0-9A-Z]{16}"; DotNet = "AKIA[0-9A-Z]{16}"; IgnoreCase = $false },
    [pscustomobject]@{ Name = "GitHubToken"; Git = "gh[pousr]_[A-Za-z0-9]{30,}"; DotNet = "gh[pousr]_[A-Za-z0-9]{30,}"; IgnoreCase = $false },
    [pscustomobject]@{ Name = "GitHubPat"; Git = "github_pat_[A-Za-z0-9_]{20,}"; DotNet = "github_pat_[A-Za-z0-9_]{20,}"; IgnoreCase = $false },
    [pscustomobject]@{ Name = "SlackToken"; Git = "xox[baprs]-[A-Za-z0-9-]{20,}"; DotNet = "xox[baprs]-[A-Za-z0-9-]{20,}"; IgnoreCase = $false },
    [pscustomobject]@{ Name = "NpmToken"; Git = "npm_[A-Za-z0-9]{30,}"; DotNet = "npm_[A-Za-z0-9]{30,}"; IgnoreCase = $false },
    [pscustomobject]@{ Name = "GoogleApiKey"; Git = "AIza[0-9A-Za-z_-]{30,}"; DotNet = "AIza[0-9A-Za-z_-]{30,}"; IgnoreCase = $false },
    [pscustomobject]@{ Name = "AzureAccountKey"; Git = ("Account" + "Key=[A-Za-z0-9+/=]{20,}"); DotNet = ("Account" + "Key=[A-Za-z0-9+/=]{20,}"); IgnoreCase = $true },
    [pscustomobject]@{ Name = "ConnectionPassword"; Git = ("Pass" + "word=[^;[:space:]]{4,}"); DotNet = ("Pass" + "word=[^;\s]{4,}"); IgnoreCase = $true }
)
$findings = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

Push-Location $root
try {
    # Git grep returns paths only, ensuring a detected value is never copied into CI output.
    foreach ($pattern in $patterns) {
        $arguments = @("grep", "-I", "-l", "-E")
        if ($pattern.IgnoreCase) { $arguments += "-i" }
        $arguments += @("-e", $pattern.Git, "--")
        $matches = @(& git @arguments 2>$null)
        $exitCode = $LASTEXITCODE
        if ($exitCode -gt 1) {
            throw "Secret scan failed while reading the current worktree for $($pattern.Name)."
        }
        foreach ($match in $matches) {
            [void]$findings.Add("$($pattern.Name):$match")
        }
    }

    # Include non-ignored new files because Git grep only reads tracked worktree paths.
    $newFiles = @(& git ls-files --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw "Cannot enumerate non-ignored new files for secret scanning." }
    foreach ($path in $newFiles) {
        try { $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8 -ErrorAction Stop }
        catch { continue }
        foreach ($pattern in $patterns) {
            $options = if ($pattern.IgnoreCase) { [Text.RegularExpressions.RegexOptions]::IgnoreCase } else { [Text.RegularExpressions.RegexOptions]::None }
            if ([regex]::IsMatch($content, $pattern.DotNet, $options)) {
                [void]$findings.Add("$($pattern.Name):$path")
            }
        }
    }

    if (-not $SkipHistory) {
        $commits = @(& git rev-list --all)
        if ($LASTEXITCODE -ne 0 -or $commits.Count -eq 0) {
            throw "Cannot enumerate Git history for secret scanning."
        }
        for ($offset = 0; $offset -lt $commits.Count; $offset += 50) {
            $last = [Math]::Min($offset + 49, $commits.Count - 1)
            $commitBatch = @($commits[$offset..$last])
            foreach ($pattern in $patterns) {
                $arguments = @("grep", "-I", "-l", "-E")
                if ($pattern.IgnoreCase) { $arguments += "-i" }
                $arguments += @("-e", $pattern.Git) + $commitBatch + @("--")
                $matches = @(& git @arguments 2>$null)
                $exitCode = $LASTEXITCODE
                if ($exitCode -gt 1) {
                    throw "Secret scan failed while reading Git history for $($pattern.Name)."
                }
                foreach ($match in $matches) {
                    [void]$findings.Add("$($pattern.Name):$match")
                }
            }
        }
    }
}
finally {
    Pop-Location
}

if ($findings.Count -gt 0) {
    $findings | Sort-Object | ForEach-Object { Write-Error $_ }
    throw "Secret scan found $($findings.Count) path-level finding(s); values were suppressed."
}

Write-Output "Secret scan passed for the current non-ignored worktree$(if ($SkipHistory) { '' } else { ' and available Git history' })."
$global:LASTEXITCODE = 0
