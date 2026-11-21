; Inno Setup script for the Clash of Thrones demo installer. Built by scripts\installer.ps1 (needs Inno Setup 6).
; Installs per user (no admin rights needed) to %LOCALAPPDATA%\Programs\Clash of Thrones.
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\build\windows"
#endif

[Setup]
AppId={{8783EA39-636B-450C-9D01-142DB61C7B91}
AppName=Clash of Thrones
AppVersion={#AppVersion}
AppVerName=Clash of Thrones Demo {#AppVersion}
AppPublisher=Clash of Thrones team
AppPublisherURL=https://github.com/CodTanium/ClashofThrones
AppSupportURL=https://github.com/CodTanium/ClashofThrones/issues
DefaultDirName={autopf}\Clash of Thrones
DefaultGroupName=Clash of Thrones
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\build
OutputBaseFilename=ClashOfThrones-v{#AppVersion}-Demo-Setup
SetupIconFile=..\src\icon.ico
UninstallDisplayIcon={app}\ClashOfThrones.exe
UninstallDisplayName=Clash of Thrones Demo
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
InfoBeforeFile=..\installer\demo-info.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "{#SourceDir}\ClashOfThrones.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\data_ClashOfThrones_windows_x86_64\*"; DestDir: "{app}\data_ClashOfThrones_windows_x86_64"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceDir}\README.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme

[Icons]
Name: "{group}\Clash of Thrones"; Filename: "{app}\ClashOfThrones.exe"
Name: "{group}\Uninstall Clash of Thrones"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Clash of Thrones"; Filename: "{app}\ClashOfThrones.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ClashOfThrones.exe"; Description: "Play Clash of Thrones now"; Flags: nowait postinstall skipifsilent