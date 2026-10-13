; Script generated for Enterprise POS Offline Desktop Installer
; Uses Inno Setup (Free & Industry Standard)

#define MyAppName "Enterprise POS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Enterprise Solutions"
#define MyAppURL "https://github.com/SMBK-04/POS"
#define MyAppExeName "EnterprisePOS.Desktop.exe"
#define MyAppSourceDir "C:\Users\SMBK\EnterprisePOS.Desktop\publish_desktop"

[Setup]
AppId={{C6287BF7-95E0-4E84-A152-A3644DA5449A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Enterprise POS
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFiles=
OutputDir=C:\Users\SMBK\EnterprisePOS.Desktop\installer_output
OutputBaseFilename=EnterprisePOS_Setup_v1.0.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\Launch_Silent.vbs"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\Launch_Silent.vbs"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\Launch_Silent.vbs"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: shellexec postinstall skipifsilent
