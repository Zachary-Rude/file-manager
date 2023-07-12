#define MyAppName "Files"
#define MyAppVersion "1.1.8"
#define MyAppFileNameVersion StringChange(MyAppVersion, ".", "_")
#define MyAppPublisher "Zach, Inc."
#define MyAppExeName "Files.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{1F21B3B6-0C9B-45E2-8D78-85FEC18F3EE9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppPublisher}\{#MyAppName}
DisableProgramGroupPage=no
DefaultGroupName=Files
LicenseFile=C:\Users\zacha\Documents\License Agreement.rtf
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir=C:\Users\zacha\source\repos\Files\Files\Inno
OutputBaseFilename=Files_{#MyAppFileNameVersion}_Setup
SetupIconFile=compiler:SetupClassicIcon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=classic
UninstallDisplayIcon=C:\Users\zacha\source\repos\Files\Files\bin\Debug\{#MyAppExeName},0
UninstallDisplayName={#MyAppName}
WizardImageFile=compiler:WizClassicImage.bmp 
WizardSmallImageFile=compiler:WizClassicSmallImage.bmp
DisableWelcomePage=no

[Code]
procedure InitializeWizard();
begin
  { Hide radio buttons and pre-select "accept", to enable "next" button }
  WizardForm.LicenseAcceptedRadio.Checked := True;
  WizardForm.LicenseAcceptedRadio.Visible := False;
  WizardForm.LicenseNotAcceptedRadio.Visible := False;
  WizardForm.LicenseMemo.Height :=
    WizardForm.LicenseNotAcceptedRadio.Top +
    WizardForm.LicenseNotAcceptedRadio.Height -
    WizardForm.LicenseMemo.Top - ScaleY(5);
end;
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpReady then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall)
  else if CurPageID = wpFinished then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonFinish)
  else if CurPageID = wpLicense then
    WizardForm.NextButton.Caption := '&I agree'
  else
    WizardForm.NextButton.Caption := '&Next >';
  WizardForm.BackButton.Caption := '< &Back';
end;

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon} (recommended)"; GroupDescription: "{cm:AdditionalIcons}"
Name: "uninsicon"; Description: "Create an Uninstall icon in Start menu (recommended)"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "C:\Users\zacha\source\repos\Files\Files\bin\Debug\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\zacha\source\repos\Files\Files\bin\Debug\ExpTreeLib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\zacha\source\repos\Files\Files\bin\Debug\Files.exe.config"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Messages]
WelcomeLabel2=Setup will guide you through the installation of {#MyAppName}.%n%nIt is recommended that you close all other applications before starting Setup. This will make it possible to update relevant system files without having to reboot your computer.
ClickNext=Click Next to continue.
SetupWindowTitle=%1 Setup
SetupAppTitle=%1 Setup  
BeveledLabel=Powered by Inno Setup

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{group}\{cm:UninstallProgram, {#MyAppName}}"; Filename: "{uninstallexe}"; Tasks: uninsicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

