#define MyAppName "SonglistSpinner"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "wowfood"
#define MyMsixFile "installer_output\SonglistSpinner_1.1.0.0_x64.msix"
#define MyMsixFileName "SonglistSpinner_1.1.0.0_x64.msix"
#define MyCertFile "SonglistSpinner.cer"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppVerName={#MyAppName} {#MyAppVersion}
; MSIX manages its own install location — no DefaultDirName needed
CreateAppDir=no
OutputBaseFilename=SonglistSpinnerSetup
OutputDir=installer_output
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
; MSIX registers its own uninstaller in Settings > Apps
Uninstallable=no
CreateUninstallRegKey=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#MyCertFile}"; Flags: dontcopy
Source: "{#MyMsixFile}"; Flags: dontcopy

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  PsArgs: String;
begin
  if CurStep = ssInstall then
  begin
    // Import signing certificate into Trusted Root so MSIX signature validates
    WizardForm.StatusLabel.Caption := 'Configuring security certificate...';
    ExtractTemporaryFile('{#MyCertFile}');
    PsArgs := '-NonInteractive -ExecutionPolicy Bypass -Command ' +
      '"Import-Certificate -FilePath ''' + ExpandConstant('{tmp}\{#MyCertFile}') + ''' ' +
      '-CertStoreLocation Cert:\LocalMachine\Root"';
    Exec('powershell.exe', PsArgs, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    // Launch the MSIX through the normal Windows installer UI
    WizardForm.StatusLabel.Caption := 'Launching {#MyAppName} installer...';
    ExtractTemporaryFile('{#MyMsixFileName}');
    ShellExec('open', ExpandConstant('{tmp}\{#MyMsixFileName}'), '', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
  end;
end;
