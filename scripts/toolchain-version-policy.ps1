# Module purpose: Owns DB-Notifier's bounded stable toolchain ranges and validates their repository representations.
#Requires -Version 7.0

Set-StrictMode -Version Latest

function Get-DBNotifierToolchainPolicy {
    <#
    .SYNOPSIS
    Returns the canonical stable toolchain compatibility contract.

    .OUTPUTS
    PSCustomObject containing the .NET, Node.js, npm and NVM policy values.

    .NOTES
    Lower bounds retain the validated baseline while exclusive upper bounds
    prevent automatic host updates from crossing an unvalidated release line.
    #>
    [OutputType([pscustomobject])]
    param()

    return [pscustomobject]@{
        DotNetMinimum = '10.0.302'
        DotNetRange = '>=10.0.302 <10.1.0'
        DotNetRollForward = 'latestFeature'
        NodeRange = '>=24.18.0 <25.0.0'
        NpmRange = '>=11.16.0 <12.0.0'
        NvmSelector = '24'
    }
}

function Get-DBNotifierRequiredProperty {
    <#
    .SYNOPSIS
    Reads one required object property without PowerShell's permissive missing-property behaviour.

    .PARAMETER InputObject
    Parsed policy object that must own the property.

    .PARAMETER Name
    Case-sensitive property name. An empty name is valid for npm's root package metadata.

    .PARAMETER Context
    Sanitised source description used in a failure message.

    .OUTPUTS
    The required property value.

    .NOTES
    A missing property is a repository-policy failure, not a compatible runtime outcome.
    #>
    param(
        [Parameter(Mandatory)]
        [object]$InputObject,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Context
    )

    if ($InputObject -is [System.Collections.IDictionary]) {
        if (-not $InputObject.Contains($Name)) {
            throw "$Context is missing required property '$Name'."
        }
        return $InputObject[$Name]
    }

    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property) {
        throw "$Context is missing required property '$Name'."
    }
    return $property.Value
}

function ConvertTo-DBNotifierStableVersion {
    <#
    .SYNOPSIS
    Parses one canonical stable three-component version.

    .PARAMETER Version
    Version text in canonical major.minor.patch form.

    .PARAMETER Context
    Sanitised description used in a failure message.

    .OUTPUTS
    System.Version containing exactly three non-negative components.

    .NOTES
    Prerelease labels, build metadata, abbreviated forms and leading zeroes are rejected.
    #>
    [OutputType([version])]
    param(
        [Parameter(Mandatory)]
        [string]$Version,

        [Parameter(Mandatory)]
        [string]$Context
    )

    $match = [regex]::Match(
        $Version,
        '^(?<major>0|[1-9][0-9]*)[.](?<minor>0|[1-9][0-9]*)[.](?<patch>0|[1-9][0-9]*)$',
        [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success) {
        throw "$Context must be a stable canonical major.minor.patch version."
    }

    try {
        return [version]::new(
            [int]::Parse($match.Groups['major'].Value, [System.Globalization.CultureInfo]::InvariantCulture),
            [int]::Parse($match.Groups['minor'].Value, [System.Globalization.CultureInfo]::InvariantCulture),
            [int]::Parse($match.Groups['patch'].Value, [System.Globalization.CultureInfo]::InvariantCulture))
    }
    catch {
        throw "$Context contains a version component outside the supported numeric range."
    }
}

function Get-DBNotifierVersionRange {
    <#
    .SYNOPSIS
    Parses the canonical inclusive-lower and exclusive-upper compatibility syntax.

    .PARAMETER Range
    Range text in the form >=major.minor.patch <major.minor.patch.

    .PARAMETER Context
    Sanitised description used in a failure message.

    .OUTPUTS
    PSCustomObject containing Lower, Upper and Text values.

    .NOTES
    Empty and inverted ranges fail as repository-policy errors.
    #>
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string]$Range,

        [Parameter(Mandatory)]
        [string]$Context
    )

    $match = [regex]::Match(
        $Range,
        '^>=(?<lower>[^ ]+) <(?<upper>[^ ]+)$',
        [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success) {
        throw "$Context must use the canonical '>=minimum <upper-bound' syntax."
    }
    $lower = ConvertTo-DBNotifierStableVersion `
        -Version $match.Groups['lower'].Value `
        -Context "$Context lower bound"
    $upper = ConvertTo-DBNotifierStableVersion `
        -Version $match.Groups['upper'].Value `
        -Context "$Context upper bound"
    if ($lower.CompareTo($upper) -ge 0) {
        throw "$Context must have an exclusive upper bound greater than its lower bound."
    }

    return [pscustomobject]@{
        Lower = $lower
        Upper = $upper
        Text = $Range
    }
}

function Test-DBNotifierVersionInRange {
    <#
    .SYNOPSIS
    Tests whether one stable version is inside a bounded compatibility range.

    .PARAMETER Version
    Candidate stable version.

    .PARAMETER Range
    Canonical inclusive-lower and exclusive-upper range.

    .OUTPUTS
    System.Boolean; false also covers malformed candidate or range text.
    #>
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [string]$Version,

        [Parameter(Mandatory)]
        [string]$Range
    )

    try {
        $candidate = ConvertTo-DBNotifierStableVersion -Version $Version -Context 'Candidate version'
        $policy = Get-DBNotifierVersionRange -Range $Range -Context 'Compatibility range'
        return $candidate.CompareTo($policy.Lower) -ge 0 -and
            $candidate.CompareTo($policy.Upper) -lt 0
    }
    catch {
        return $false
    }
}

function Assert-DBNotifierVersionInRange {
    <#
    .SYNOPSIS
    Fails closed when an observed tool version is outside its declared range.

    .PARAMETER Version
    Observed stable tool version.

    .PARAMETER Range
    Canonical compatibility range from the repository contract.

    .PARAMETER ToolName
    Sanitised tool name used in the dependency diagnostic.

    .OUTPUTS
    None.

    .NOTES
    Malformed range text is reported as a policy defect; malformed or incompatible
    observed versions produce the executable DEPENDENCY_UNREADY stop code.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Version,

        [Parameter(Mandatory)]
        [string]$Range,

        [Parameter(Mandatory)]
        [string]$ToolName
    )

    $policy = Get-DBNotifierVersionRange -Range $Range -Context "$ToolName compatibility range"
    try {
        $candidate = ConvertTo-DBNotifierStableVersion -Version $Version -Context "$ToolName observed version"
    }
    catch {
        throw "DEPENDENCY_UNREADY: $ToolName '$Version' is not a supported stable version for '$Range'."
    }
    if ($candidate.CompareTo($policy.Lower) -lt 0 -or
        $candidate.CompareTo($policy.Upper) -ge 0) {
        throw "DEPENDENCY_UNREADY: $ToolName '$Version' is outside the compatible range '$Range'."
    }
}

function Get-DBNotifierDotNetSdkPolicy {
    <#
    .SYNOPSIS
    Validates global.json against the canonical bounded .NET 10.0 contract.

    .PARAMETER GlobalJsonPath
    Absolute path to the repository global.json file.

    .OUTPUTS
    PSCustomObject containing Minimum, Range and RollForward.

    .NOTES
    The global.json version remains the minimum selection baseline; latestFeature
    permits later stable patches and feature bands only within .NET 10.0.
    #>
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string]$GlobalJsonPath
    )

    $canonical = Get-DBNotifierToolchainPolicy
    $document = Get-Content -LiteralPath $GlobalJsonPath -Raw | ConvertFrom-Json
    $sdk = Get-DBNotifierRequiredProperty -InputObject $document -Name 'sdk' -Context 'global.json'
    $minimum = [string](Get-DBNotifierRequiredProperty -InputObject $sdk -Name 'version' -Context 'global.json sdk')
    $rollForward = [string](Get-DBNotifierRequiredProperty -InputObject $sdk -Name 'rollForward' -Context 'global.json sdk')
    $allowPrerelease = Get-DBNotifierRequiredProperty -InputObject $sdk -Name 'allowPrerelease' -Context 'global.json sdk'
    if ($minimum -cne $canonical.DotNetMinimum -or
        $rollForward -cne $canonical.DotNetRollForward -or
        $allowPrerelease -isnot [bool] -or
        $allowPrerelease) {
        throw 'global.json diverges from the bounded stable .NET SDK policy.'
    }
    [void](Get-DBNotifierVersionRange -Range $canonical.DotNetRange -Context '.NET SDK compatibility range')

    return [pscustomobject]@{
        Minimum = $minimum
        Range = $canonical.DotNetRange
        RollForward = $rollForward
    }
}

function Get-DBNotifierDashboardToolchainPolicy {
    <#
    .SYNOPSIS
    Validates the Dashboard manifest, lock metadata and NVM selector as one contract.

    .PARAMETER RepositoryRoot
    Absolute DB-Notifier repository root.

    .OUTPUTS
    PSCustomObject containing the canonical NodeRange, NpmRange and NvmSelector.

    .NOTES
    The dependency graph and integrity records remain independent of this engine-metadata check.
    #>
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot
    )

    $canonical = Get-DBNotifierToolchainPolicy
    $dashboardRoot = Join-Path $RepositoryRoot 'src/DBNotifier.Dashboard.Web'
    $manifest = Get-Content -LiteralPath (Join-Path $dashboardRoot 'package.json') -Raw |
        ConvertFrom-Json
    $engines = Get-DBNotifierRequiredProperty -InputObject $manifest -Name 'engines' -Context 'package.json'
    $nodeRange = [string](Get-DBNotifierRequiredProperty -InputObject $engines -Name 'node' -Context 'package.json engines')
    $npmRange = [string](Get-DBNotifierRequiredProperty -InputObject $engines -Name 'npm' -Context 'package.json engines')
    if ($nodeRange -cne $canonical.NodeRange -or $npmRange -cne $canonical.NpmRange) {
        throw 'package.json engines diverge from the canonical compatible toolchain ranges.'
    }
    [void](Get-DBNotifierVersionRange -Range $nodeRange -Context 'Node.js compatibility range')
    [void](Get-DBNotifierVersionRange -Range $npmRange -Context 'npm compatibility range')

    if ($null -ne $manifest.PSObject.Properties['packageManager']) {
        throw 'package.json must not encode an exact packageManager version.'
    }
    $devEngines = Get-DBNotifierRequiredProperty -InputObject $manifest -Name 'devEngines' -Context 'package.json'
    $runtime = Get-DBNotifierRequiredProperty -InputObject $devEngines -Name 'runtime' -Context 'package.json devEngines'
    $packageManager = Get-DBNotifierRequiredProperty -InputObject $devEngines -Name 'packageManager' -Context 'package.json devEngines'
    if ([string](Get-DBNotifierRequiredProperty -InputObject $runtime -Name 'name' -Context 'package.json devEngines.runtime') -cne 'node' -or
        [string](Get-DBNotifierRequiredProperty -InputObject $runtime -Name 'version' -Context 'package.json devEngines.runtime') -cne $nodeRange -or
        [string](Get-DBNotifierRequiredProperty -InputObject $runtime -Name 'onFail' -Context 'package.json devEngines.runtime') -cne 'error' -or
        [string](Get-DBNotifierRequiredProperty -InputObject $packageManager -Name 'name' -Context 'package.json devEngines.packageManager') -cne 'npm' -or
        [string](Get-DBNotifierRequiredProperty -InputObject $packageManager -Name 'version' -Context 'package.json devEngines.packageManager') -cne $npmRange -or
        [string](Get-DBNotifierRequiredProperty -InputObject $packageManager -Name 'onFail' -Context 'package.json devEngines.packageManager') -cne 'error') {
        throw 'package.json devEngines must fail closed with the same compatible Node.js and npm ranges.'
    }

    $nvmSelector = (Get-Content -LiteralPath (Join-Path $RepositoryRoot '.nvmrc') -Raw).Trim()
    if ($nvmSelector -cne $canonical.NvmSelector) {
        throw '.nvmrc must select the compatible Node.js major release line.'
    }

    $lock = Get-Content -LiteralPath (Join-Path $dashboardRoot 'package-lock.json') -Raw |
        ConvertFrom-Json -AsHashtable
    $packages = Get-DBNotifierRequiredProperty -InputObject $lock -Name 'packages' -Context 'package-lock.json'
    $rootPackage = Get-DBNotifierRequiredProperty -InputObject $packages -Name '' -Context 'package-lock.json packages'
    $lockEngines = Get-DBNotifierRequiredProperty -InputObject $rootPackage -Name 'engines' -Context 'package-lock.json root'
    if ([string](Get-DBNotifierRequiredProperty -InputObject $lockEngines -Name 'node' -Context 'package-lock.json root engines') -cne $nodeRange -or
        [string](Get-DBNotifierRequiredProperty -InputObject $lockEngines -Name 'npm' -Context 'package-lock.json root engines') -cne $npmRange) {
        throw 'package-lock.json root engine metadata diverges from package.json.'
    }

    return [pscustomobject]@{
        NodeRange = $nodeRange
        NpmRange = $npmRange
        NvmSelector = $nvmSelector
    }
}
