; Microled NFe ambiente local (on-prem) — Inno Setup
; Build: scripts\Build-OnPrem-Installer.ps1

#ifndef PackageDir
  #define PackageDir "..\dist\onprem-package"
#endif
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist\installers"
#endif
#ifndef SetupBaseName
  #define SetupBaseName "Microled-NFe-AmbienteLocal-1.0.0"
#endif

#define MyAppName "Microled NFe Ambiente Local"
#define MyAppPublisher "Microled"
#define MyAppExeName "Microled.Nfe.DesktopLauncher.exe"
#define MyInstallDir "{autopf}\Microled\NfeOnPrem"
#define MyDataDir "{commonappdata}\Microled\Nfe"
#define MyFirewallApi "Microled NFe API (TCP 5249)"
#define MyFirewallAgent "Microled NFe Local Agent (TCP 5278)"

[Setup]
AppId={{B7E4D1A0-2C8F-4E91-9A33-6F0C1B8D4E27}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={#MyInstallDir}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename={#SetupBaseName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PackageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir o painel agora"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#MyFirewallApi}"""; Flags: runhidden; RunOnceId: "RemoveFwApi"
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#MyFirewallAgent}"""; Flags: runhidden; RunOnceId: "RemoveFwAgent"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  DataRoot: string;
begin
  if CurStep = ssPostInstall then
  begin
    DataRoot := ExpandConstant('{#MyDataDir}');
    ForceDirectories(DataRoot);
    ForceDirectories(DataRoot + '\launcher');
    ForceDirectories(DataRoot + '\postgres');
    ForceDirectories(DataRoot + '\localagent');
    ForceDirectories(DataRoot + '\localagent\logs');
    ForceDirectories(DataRoot + '\localagent\RpsOut');
    ForceDirectories(DataRoot + '\localagent\Validate');

    Exec('icacls.exe',
      '"' + DataRoot + '" /grant *S-1-5-32-545:(OI)(CI)M /T /C',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    Exec('netsh.exe',
      'advfirewall firewall add rule name="' + '{#MyFirewallApi}' + '" dir=in action=allow protocol=TCP localport=5249',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('netsh.exe',
      'advfirewall firewall add rule name="' + '{#MyFirewallAgent}' + '" dir=in action=allow protocol=TCP localport=5278',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    Exec('netsh.exe',
      'advfirewall firewall delete rule name="' + '{#MyFirewallApi}' + '"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('netsh.exe',
      'advfirewall firewall delete rule name="' + '{#MyFirewallAgent}' + '"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

[Messages]
brazilianportuguese.CompletedLabel=O ambiente local da Microled NFS-e foi instalado.%n%nAbra o atalho, indique o arquivo Access (NF.mdb), clique em Iniciar ambiente e escolha o certificado digital.
