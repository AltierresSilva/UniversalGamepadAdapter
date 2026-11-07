#define AppName "Universal Gamepad Adapter"
#define AppVersion "1.0.0"
#define AppExe "UniversalGamepadAdapter.exe"

[Setup]
AppId={{2CB96C97-39E2-4D90-9CAC-C224E18D4CEB}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Universal Gamepad Adapter
DefaultDirName={autopf}\Universal Gamepad Adapter
DefaultGroupName=Universal Gamepad Adapter
DisableProgramGroupPage=yes
OutputDir=..\artifacts\Installer
OutputBaseFilename=UGA-{#AppVersion}-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
MinVersion=10.0.22000
WizardStyle=modern
SetupIconFile=..\src\UGA.App\Assets\UGA.ico
UninstallDisplayIcon={app}\{#AppExe}
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
VersionInfoVersion=1.0.0.0

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "..\artifacts\Release\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Desinstalar UGA"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0"; Description: "Abrir o fornecedor oficial para instalar ViGEmBus (necessário para emular)"; Flags: postinstall shellexec skipifsilent unchecked; Check: NeedsViGEm
Filename: "{app}\{#AppExe}"; Description: "Abrir Universal Gamepad Adapter"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallDelete]
Type: files; Name: "{userstartup}\UniversalGamepadAdapter.cmd"

[Code]
var DriverPage: TWizardPage;
    DriverText: TNewStaticText;

function NeedsViGEm: Boolean;
begin
  Result := not RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\ViGEmBus');
end;

procedure InitializeWizard;
begin
  DriverPage := CreateCustomPage(wpSelectTasks, 'Controle virtual e drivers', 'A aplicação funciona offline após configurar o driver.');
  DriverText := TNewStaticText.Create(DriverPage);
  DriverText.Parent := DriverPage.Surface;
  DriverText.SetBounds(0, 0, DriverPage.SurfaceWidth, DriverPage.SurfaceHeight);
  DriverText.WordWrap := True;
  if NeedsViGEm then
    DriverText.Caption := 'ViGEmBus não detectado. Ao final, você poderá abrir o fornecedor oficial e instalar o pacote assinado com seu consentimento.'
  else
    DriverText.Caption := 'ViGEmBus detectado. O instalador preservará o driver existente. O UGA verificará o acesso ao barramento e informará falhas ao iniciar a emulação.';
  DriverText.Caption := DriverText.Caption + #13#10#13#10 +
    'ViGEmBus é um backend legado descontinuado. HidHide é opcional para evitar entradas duplicadas. Ambos são configurados no UGA.' + #13#10#13#10 +
    'Nenhum driver é incluído, baixado ou instalado automaticamente. Use somente os links oficiais do guia DRIVERS.md. Se o fornecedor solicitar reinicialização, reinicie o Windows antes de testar.' + #13#10#13#10 +
    'A desinstalação do UGA não remove drivers compartilhados. Desfaça as alterações HidHide pelo UGA antes de desinstalar.';
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var DataRoot: String;
begin
  if CurUninstallStep <> usPostUninstall then Exit;
  if UninstallSilent then Exit;
  DataRoot := ExpandConstant('{localappdata}\UniversalGamepadAdapter');
  if not DirExists(DataRoot) then Exit;
  if FileExists(DataRoot + '\hidhide-changes.json') then begin
    MsgBox('As configurações e o backup HidHide foram preservados. Reinstale/abra o UGA para desfazer somente as alterações dele, ou reexiba o HID físico no cliente oficial. Reconecte o controle.', mbInformation, MB_OK);
    Exit;
  end;
  if MsgBox('Deseja remover as configurações e os perfis UGA do usuário atual? Escolha Não para preservá-los.', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
    DelTree(DataRoot, True, True, True);
end;
