#define AppName "PgNotifier"
#ifndef SourceDir
  #define SourceDir "..\..\dist\package"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\dist\installers"
#endif

[Setup]
AppId={{3E7DDA86-B53C-4265-A1C3-2B7A87620B91}
AppName={#AppName}
AppVersion=1.1.0
AppPublisher=Open Source
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename=PgNotifier-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

[Tasks]
Name: "startup"; Description: "Start with Windows"; GroupDescription: "Additional options:"; Flags: unchecked
Name: "desktopicon"; Description: "Create a Desktop shortcut"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\PgNotifier.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\config\appsettings.json"; DestDir: "{commonappdata}\PgNotifier"; Flags: ignoreversion onlyifdoesntexist

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\PgNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\PgNotifier\appsettings.json"""; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\PgNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\PgNotifier\appsettings.json"""; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{commonstartup}\{#AppName}"; Filename: "{app}\PgNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\PgNotifier\appsettings.json"""; WorkingDir: "{app}"; Tasks: startup

[Run]
Filename: "{app}\PgNotifier.exe"; Parameters: "-ConfigPath ""{commonappdata}\PgNotifier\appsettings.json"""; WorkingDir: "{app}"; Description: "Run {#AppName}"; Flags: nowait postinstall skipifsilent
