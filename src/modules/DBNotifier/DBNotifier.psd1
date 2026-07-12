# Module purpose: Provides DBNotifier for the legacy-compatible DB-Notifier tooling without changing database services implicitly.
@{
    RootModule        = 'DBNotifier.psm1'
    ModuleVersion     = '1.1.0'
    GUID              = '53b34684-2b1d-417a-b4cd-6f4ff4c09763'
    Author            = 'Bruno Araújo Ávila'
    CompanyName       = 'DegsTerin'
    Copyright         = 'Copyright (c) 2026 Bruno Araújo Ávila - DegsTerin'
    Description       = 'DB-Notifier PostgreSQL compatibility monitor for Windows.'
    PowerShellVersion = '5.1'
    FunctionsToExport = @('Start-DBNotifierApplication', 'Start-PgNotifierApplication')
    CmdletsToExport   = @()
    VariablesToExport = @()
    AliasesToExport   = @()
}
