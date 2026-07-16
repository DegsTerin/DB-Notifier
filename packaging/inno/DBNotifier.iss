; Module purpose: Defines DBNotifier packaging metadata for DB-Notifier artefacts.
#define AppName "DB-Notifier"
#define AppDisplayName "DB Notifier"
#ifndef SourceDir
  #define SourceDir "..\..\dist\package"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\dist\installers"
#endif

[Setup]
AppId={{7B133F29-9D33-4F17-A07E-69FB64685EFE}
AppName={#AppDisplayName}
AppVersion=1.1.1
AppPublisher=DegsTerin
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppDisplayName}
OutputDir={#OutputDir}
OutputBaseFilename=DBNotifier-Setup
SetupIconFile=..\..\src\DBNotifier.Desktop.Wpf\Assets\DBNotifier.ico
UninstallDisplayIcon={app}\DBNotifier.exe
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

[Tasks]
Name: "startup"; Description: "Start with Windows"; GroupDescription: "Additional options:"; Flags: unchecked
Name: "desktopicon"; Description: "Create a Desktop shortcut"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\DBNotifier.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Assets\*.ico"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "{#SourceDir}\config\appsettings.json"; DestDir: "{commonappdata}\DB-Notifier"; Flags: ignoreversion onlyifdoesntexist

[Icons]
Name: "{autoprograms}\{#AppDisplayName}"; Filename: "{app}\DBNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\DB-Notifier\appsettings.json"""; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppDisplayName}"; Filename: "{app}\DBNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\DB-Notifier\appsettings.json"""; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{commonstartup}\{#AppDisplayName}"; Filename: "{app}\DBNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\DB-Notifier\appsettings.json"""; WorkingDir: "{app}"; Tasks: startup

[Run]
Filename: "{app}\DBNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\DB-Notifier\appsettings.json"""; WorkingDir: "{app}"; Description: "Run {#AppDisplayName}"; Flags: nowait postinstall skipifsilent
