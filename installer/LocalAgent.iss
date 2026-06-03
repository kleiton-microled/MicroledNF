; Microled NFe Local Agent — Inno Setup script
; Build: scripts\Build-LocalAgent-Installer.ps1

#ifndef PublishDir
  #define PublishDir "..\dist\localagent-publish\microled"
#endif
#ifndef ClientId
  #define ClientId "microled"
#endif
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist\installers"
#endif
#ifndef SetupBaseName
  #define SetupBaseName "Microled-NFe-LocalAgent-microled-1.0.0"
#endif

#define MyAppName "Microled NFe Local Agent"
#define MyAppPublisher "Microled"
#define MyAppExeName "Microled.Nfe.LocalAgent.Api.exe"
#define MyInstallDir "{autopf}\Microled\NfeLocalAgent"
#define MyDataDir "{commonappdata}\Microled\Nfe\localagent"
#define MyFirewallRule "Microled NFe Local Agent (TCP 5278)"

[Setup]
AppId={{A8F3C2E1-9B4D-4F6A-8C1E-2D5F7A9B3C4E}
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
Name: "startup"; Description: "Iniciar automaticamente ao entrar no Windows (recomendado)"; GroupDescription: "Inicialização:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "StartLocalAgent.vbs"; DestDir: "{app}"; Flags: ignoreversion
Source: "StartLocalAgent.cmd"; DestDir: "{app}"; Flags: ignoreversion
Source: "health-check.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\StartLocalAgent.vbs"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppName} (console)"; Filename: "{app}\StartLocalAgent.cmd"; WorkingDir: "{app}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\StartLocalAgent.vbs"; WorkingDir: "{app}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "MicroledNfeLocalAgent"; ValueData: "wscript.exe ""{app}\StartLocalAgent.vbs"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\health-check.ps1"" -Port 5278"; Description: "Verificar se o agente responde em http://localhost:5278"; Flags: postinstall nowait skipifsilent
Filename: "{sys}\wscript.exe"; Parameters: """{app}\StartLocalAgent.vbs"""; Description: "Iniciar {#MyAppName} agora"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#MyFirewallRule}"""; Flags: runhidden; RunOnceId: "RemoveFirewall"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  DataRoot, RpsOut, ValidateDir, LogsDir: string;
begin
  if CurStep = ssPostInstall then
  begin
    DataRoot := ExpandConstant('{#MyDataDir}');
    RpsOut := DataRoot + '\RpsOut';
    ValidateDir := DataRoot + '\Validate';
    LogsDir := DataRoot + '\logs';
    ForceDirectories(DataRoot);
    ForceDirectories(RpsOut);
    ForceDirectories(ValidateDir);
    ForceDirectories(LogsDir);

    Exec('netsh.exe',
      'advfirewall firewall add rule name="' + '{#MyFirewallRule}' + '" dir=in action=allow protocol=TCP localport=5278',
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
      'advfirewall firewall delete rule name="' + '{#MyFirewallRule}' + '"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
end;

procedure DeinitializeSetup();
begin
end;

[Messages]
brazilianportuguese.CompletedLabel=O Microled NFe Local Agent foi instalado.%n%nNa primeira emissão de NFS-e, confirme o PIN do certificado digital (token A3) quando solicitado.%n%nO agente escuta em http://localhost:5278
