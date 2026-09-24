#define AppName "LightBulb EyeCare"
#define AppVersion GetEnv("INSTALLER_APP_VERSION")

[Setup]
AppId={{3C7A9E14-5B62-4D8F-9A31-6E5C2B7D4F08}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher="duhaoda"
AppPublisherURL="https://github.com/duhaoda/lightbulb-eyecare"
AppSupportURL="https://github.com/duhaoda/lightbulb-eyecare/issues"
AppUpdatesURL="https://github.com/duhaoda/lightbulb-eyecare/releases"
AppMutex=LightBulb_Identity
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
DisableWelcomePage=yes
DisableProgramGroupPage=no
DisableReadyPage=yes
SetupIconFile=..\favicon.ico
UninstallDisplayIcon={app}\LightBulb.exe
OutputDir=bin\
OutputBaseFilename=LightBulb-Installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: ".installed"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\License.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\NOTICE.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "Source\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\LightBulb.exe"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{group}\{#AppName} on Github"; Filename: "https://github.com/duhaoda/lightbulb-eyecare"

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows NT\CurrentVersion\ICM"; ValueType: dword; ValueName: "GdiICMGammaRange"; ValueData: "256"

[Run]
Filename: "{app}\LightBulb.exe"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Name: "{userappdata}\LightBulb"; Type: filesandordirs
