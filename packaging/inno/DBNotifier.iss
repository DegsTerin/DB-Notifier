#define AppName "DB-Notifier"
#ifndef SourceDir
  #define SourceDir "..\..\dist\package"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\dist\installers"
#endif

[Setup]
AppId={{7B133F29-9D33-4F17-A07E-69FB64685EFE}
AppName={#AppName}
AppVersion=1.1.0
AppPublisher=Open Source
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename=DBNotifier-Setup
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
Source: "{#SourceDir}\config\appsettings.json"; DestDir: "{commonappdata}\DB-Notifier"; Flags: ignoreversion onlyifdoesntexist

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\DBNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\DB-Notifier\appsettings.json"""; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\DBNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\DB-Notifier\appsettings.json"""; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{commonstartup}\{#AppName}"; Filename: "{app}\DBNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\DB-Notifier\appsettings.json"""; WorkingDir: "{app}"; Tasks: startup

[Run]
Filename: "{app}\DBNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\DB-Notifier\appsettings.json"""; WorkingDir: "{app}"; Description: "Run {#AppName}"; Flags: nowait postinstall skipifsilent
