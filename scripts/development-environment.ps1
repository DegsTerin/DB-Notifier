# Module purpose: Removes inherited product activators and credentials from isolated development runner processes.
#Requires -Version 7.0

function Remove-DBNotifierInheritedEnvironment {
    <#
    .SYNOPSIS
    Removes hazardous inherited environment entries from a child process without reading their values.

    .PARAMETER StartInfo
    Shell-free child-process configuration whose private environment copy is sanitised.

    .OUTPUTS
    None.

    .NOTES
    Throws `ISOLATION_FAILURE` when shell execution is enabled because a private child environment cannot then be guaranteed.
    Prefix matching is deliberately case-insensitive so the boundary is equivalent on Windows and Linux.
    The parent process environment is never mutated.
    #>
    param(
        [Parameter(Mandatory)]
        [System.Diagnostics.ProcessStartInfo]$StartInfo
    )

    if ($StartInfo.UseShellExecute) {
        throw 'ISOLATION_FAILURE: Environment isolation requires UseShellExecute to remain disabled.'
    }

    $hazardousPrefixes = @(
        'ASPNETCORE_',
        'CONNECTIONSTRINGS__',
        'DASHBOARDTVSANDBOX__',
        'DASHBOARDTVSIGNALRSANDBOX__',
        'DBN_',
        'DBNOTIFIER_',
        'HUMANAUTHENTICATION__',
        'KESTREL__',
        'RECONCILEDLOCALNOTIFICATIONSANDBOX__',
        'VITE_DB_NOTIFIER_')
    $hazardousExactNames = @(
        'AZURE_OPENAI_API_KEY',
        'DOTNET_ENVIRONMENT',
        'OPENAI_API_KEY')

    foreach ($variableName in @($StartInfo.Environment.Keys)) {
        $normalisedName = ([string]$variableName).ToUpperInvariant()
        $isHazardous = $hazardousExactNames -ccontains $normalisedName
        if (-not $isHazardous) {
            $isHazardous = @($hazardousPrefixes | Where-Object {
                    $normalisedName.StartsWith(
                        $_,
                        [System.StringComparison]::Ordinal)
                }).Count -gt 0
        }
        if ($isHazardous) {
            [void]$StartInfo.Environment.Remove([string]$variableName)
        }
    }
}
