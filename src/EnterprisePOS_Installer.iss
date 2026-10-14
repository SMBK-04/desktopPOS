; Script generated for Enterprise POS Offline Desktop Installer
; Uses Inno Setup (Free & Industry Standard)

#define MyAppName "Enterprise POS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Enterprise Solutions"
#define MyAppURL "https://github.com/SMBK-04/desktopPOS"
#define MyAppExeName "EnterprisePOS.Desktop.exe"
#define MyAppSourceDir "C:\Users\SMBK\EnterprisePOS_Desktop_App\publish_selfcontained"

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
OutputDir=C:\Users\SMBK\EnterprisePOS_Desktop_App\installer_output
OutputBaseFilename=EnterprisePOS_Setup_v1.0.0
SetupIconFile=C:\Users\SMBK\EnterprisePOS_Desktop_App\src\app.ico
UninstallIconFile=C:\Users\SMBK\EnterprisePOS_Desktop_App\src\app.ico
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
Source: "C:\Users\SMBK\EnterprisePOS_Desktop_App\src\app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
