using System.Diagnostics;
using System.Text;
using UGA.Core;
using UGA.Core.Storage;
using UGA.Infrastructure.Diagnostics;
using UGA.Infrastructure.HidHide;
using UGA.Infrastructure.Virtual;

namespace UGA.App;

internal sealed partial class MainForm : Form
{
    private readonly ControllerEngine engine;
    private readonly JsonStore store;
    private readonly HidHideService hidHide;
    private AppSettings settings;
    private MappingProfile? editing;
    private readonly Label physical = Theme.Label(), connection = Theme.Label(), virtualStatus = Theme.Label(),
        driverStatus = Theme.Label("Verificando ViGEmBus…"), activity = Theme.Label(), notice = Theme.Label(),
        rightSource = Theme.Label(), mapStatus = Theme.Label(), hidStatus = Theme.Label();
    private readonly ModernButton toggle;
    private readonly ComboBox deviceSelect = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 470 };
    private readonly ControllerView mainView = new(), mapView = new();
    private readonly ListView mappingList = new() { View = View.Details, FullRowSelect = true, MultiSelect = false, Dock = DockStyle.Fill, HideSelection = false };
    private readonly NumericUpDown deadzone = new() { Minimum = 0, Maximum = 30, Value = 8, Width = 85 }, frequency = new() { Minimum = 125, Maximum = 250, Value = 125, Width = 85 };
    private readonly CheckBox autoStart = new() { Text = "Iniciar emulação ao conectar um controle configurado", AutoSize = true },
        trayOption = new() { Text = "Minimizar para a bandeja", AutoSize = true }, startup = new() { Text = "Iniciar UGA com o Windows (usuário atual)", AutoSize = true };
    private readonly CheckBox[] inversions = [new() { Text = "Inverter LX", AutoSize = true }, new() { Text = "Inverter LY", AutoSize = true }, new() { Text = "Inverter RX", AutoSize = true }, new() { Text = "Inverter RY", AutoSize = true }];
    private readonly TextBox diagnostic = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, WordWrap = false };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 40 };
    private readonly NotifyIcon tray = new() { Icon = Branding.CreateIcon(), Text = "Universal Gamepad Adapter", Visible = true };
    private string driver = "Verificando", deviceListSignature = "__initial", lastLogError = "";
    private string[] conflicts = [];
    private bool exitRequested, fillingDevices;
    private int ticks;
    public MainForm(ControllerEngine engine, JsonStore store, AppSettings settings)
    {
        this.engine=engine;this.store=store;this.settings=settings;hidHide=new(store.Root);
        Theme.Configure(settings.Theme);
        Icon=Branding.CreateIcon();
        Text="Universal Gamepad Adapter";Size=new(1280,860);MinimumSize=new(1060,740);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;
        toggle=Theme.Button("Iniciar emulação",()=>{engine.RequestEmulation(!engine.Snapshot.Running);Navigate(0);},ButtonTone.Primary,"power");
        BuildShell();
        frequency.Value=settings.PollHz;autoStart.Checked=settings.AutoStart;trayOption.Checked=settings.MinimizeToTray;startup.Checked=File.Exists(StartupFile);
        deviceSelect.SelectedIndexChanged+=(_,_)=>{if(!fillingDevices&&deviceSelect.SelectedItem is DeviceChoice choice&&choice.Key.Length>0){engine.SelectDevice(choice.Key);settings=settings with{PreferredDeviceKey=choice.Key};SaveSettings();}};
        var trayMenu=new ContextMenuStrip();trayMenu.Items.Add("Abrir UGA",null,(_,_)=>RestoreWindow());trayMenu.Items.Add("Parar emulação",null,(_,_)=>engine.RequestEmulation(false));trayMenu.Items.Add("Sair",null,(_,_)=>{exitRequested=true;Close();});
        tray.ContextMenuStrip=trayMenu;tray.DoubleClick+=(_,_)=>RestoreWindow();
        Resize+=(_,_)=>{if(WindowState==FormWindowState.Minimized&&settings.MinimizeToTray)Hide();};
        FormClosing+=(_,e)=>{if(!exitRequested&&e.CloseReason==CloseReason.UserClosing&&settings.MinimizeToTray){e.Cancel=true;Hide();tray.ShowBalloonTip(2000,"UGA continua ativo","Use Sair na bandeja para encerrar e liberar o Xbox virtual.",ToolTipIcon.Info);}};
        FormClosed+=(_,_)=>{timer.Stop();timer.Dispose();tray.Visible=false;tray.Dispose();};
        HandleCreated+=(_,_)=>Theme.TitleBar(this);
        timer.Tick+=(_,_)=>UpdateUi();timer.Start();Shown+=(_,_)=>_=RefreshEnvironment();
        Theme.Apply(this);UpdateThemeControls();RefreshMappingRows();Navigate(0);
    }
    public void CloseForTest() { exitRequested = true; Close(); }
    private void RestoreWindow() { Show(); WindowState = FormWindowState.Normal; Activate(); }
    private void SaveSettings() { try { store.SaveSettings(settings); } catch (Exception ex) { Error(ex); } }
    private static string StartupFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "UniversalGamepadAdapter.cmd");
    private void SaveAllSettings()
    {
        try
        {
            settings = settings with { AutoStart = autoStart.Checked, MinimizeToTray = trayOption.Checked, StartWithWindows = startup.Checked, PollHz = (int)frequency.Value,
                CustomLedEnabled = customLed.Checked, LedColor = selectedLedColor, VibrationPercent = (int)vibration.Value };
            if (editing is not null)
            {
                editing = editing with { Deadzone = (float)deadzone.Value / 100 };
                for (int i = 0; i < 4; i++) if (editing.Bindings.TryGetValue(Target.LX + i, out var b)) editing.Bindings[Target.LX + i] = b with { Invert = inversions[i].Checked };
                SaveProfile();
            }
            if (startup.Checked)
            {
                var executable = Environment.ProcessPath!;
                if (executable.Contains('%') || executable.Contains('"')) throw new IOException("Caminho do executável incompatível com inicialização por usuário.");
                File.WriteAllText(StartupFile, "@echo off\r\nstart \"\" \"" + executable + "\"\r\n", Encoding.Default);
            }
            else if (File.Exists(StartupFile)) File.Delete(StartupFile);
            store.SaveSettings(settings); engine.Configure(settings); MessageBox.Show(this, "Configurações salvas.", "UGA");
        }
        catch (Exception ex) { Error(ex); }
    }
    private void ResetSettings()
    {
        autoStart.Checked = false; trayOption.Checked = true; frequency.Value = 125; deadzone.Value = 8;
        customLed.Checked = false; selectedLedColor = new AppSettings().LedColor; vibration.Value = 100; UpdateLedChoice();
        for (int i = 0; i < 4; i++) inversions[i].Checked = i is 1 or 3 && editing?.Bindings.GetValueOrDefault(Target.LX + i)?.Kind == InputKind.SdlAxis;
        startup.Checked = false; SaveAllSettings();
    }
    private void UpdateUi()
    {
        var s=engine.Snapshot;var d=s.Input.Device;
        UpdateOutputControls(s);
        physical.Text=d?.Name??"Nenhum controle conectado";
        connection.Text=d?.Connection??"Bluetooth / USB";
        virtualStatus.Text=s.Running?"Pronto para jogar":d is null?"Conecte seu controle":s.Error.Length>0?"Verifique a conexão":s.Profile?.Complete==true?"Tudo pronto para começar":"Configure o mapeamento";
        heroDescription.Text=s.Running?"Seus comandos estão sendo convertidos em tempo real.":d is null?"Conecte um gamepad por Bluetooth ou USB para começar.":"Inicie a emulação para disponibilizar um controle Xbox aos jogos.";
        emulationBadge.Text=s.Running?"●  ATIVA":"●  EM ESPERA";emulationBadge.ForeColor=s.Running?Theme.Green:Theme.Muted;
        outputStatus.Text=s.Running?"Xbox 360 Controller conectado":"Xbox 360 Controller • desativado";
        toggle.Text=s.Running?"Desativar emulação":"Iniciar emulação";toggle.Tone=s.Running?ButtonTone.Danger:ButtonTone.Primary;
        toggle.Enabled=d is not null&&s.Profile?.Complete==true;toggle.Invalidate();
        activity.Text=s.ActualHz>0?$"{s.ActualHz:F0} Hz":"Aguardando leitura";
        deviceMapping.Text=d is null?"Aguardando dispositivo":d.HasSdlMapping?"Layout SDL reconhecido":"Mapeamento manual necessário";
        notice.Text=s.Error.Length>0?s.Error:conflicts.Length>0?"Possível conflito: "+string.Join(", ",conflicts)+". Feche outros remapeadores antes de jogar.":store.LastRecovery;
        notice.ForeColor=s.Error.Length>0?Theme.Red:Theme.Muted;
        notice.Visible=notice.Text.Length>0;
        driverStatus.Text=driver.Contains("acessível")?"ViGEmBus acessível":driver=="Verificando"?"Verificando ViGEmBus…":"ViGEmBus indisponível";
        driverStatus.ForeColor=driver.Contains("acessível")?Theme.Green:driver=="Verificando"?Theme.Muted:Theme.Red;
        hidStatus.Text=hidHide.Installed?hidHide.HasChanges?"Alterações UGA registradas":"Instalado • configuração opcional":"Não instalado • opcional";
        hidStatus.ForeColor=Theme.Muted;
        conflictStatus.Text=conflicts.Length==0?"Nenhum remapeador detectado":"Revisar: "+string.Join(", ",conflicts);conflictStatus.ForeColor=conflicts.Length==0?Theme.Green:Theme.Red;
        sidebarState.Text=s.Running?"●  Emulação ativa":"●  "+(d is null?"Aguardando controle":"Controle conectado");sidebarState.ForeColor=d is null?Theme.Muted:Theme.Green;
        sidebarDetail.Text=driver.Contains("acessível")?"ViGEmBus disponível":"Verifique o driver";
        var signature=string.Join("|",s.Devices.Select(x=>x.Key));
        if(signature!=deviceListSignature)
        {
            fillingDevices=true;deviceSelect.Items.Clear();
            if(s.Devices.Count==0)deviceSelect.Items.Add(new DeviceChoice("","Nenhum controle"));
            foreach(var device in s.Devices)deviceSelect.Items.Add(new DeviceChoice(device.Key,device.Name));
            int selection=s.Devices.ToList().FindIndex(x=>x.Key==(settings.PreferredDeviceKey.Length>0?settings.PreferredDeviceKey:d?.Key));
            deviceSelect.SelectedIndex=selection<0&&deviceSelect.Items.Count>0?0:selection;
            fillingDevices=false;deviceListSignature=signature;
        }
        if(d is not null&&editing?.DeviceKey!=d.Key&&s.Profile is not null){editing=s.Profile.Copy();LoadProfileControls();RefreshMappingRows();}
        var preview=d is not null&&editing?.DeviceKey==d.Key?new MappingService().Map(s.Input,editing):default;
        mainView.Shape=sourceView.Shape=ControllerView.ForDevice(d);mainView.State=sourceView.State=preview;
        mapView.State=preview;outputView.State=s.Output;calibrationView.State=preview;
        rightSource.Text=RightAxis(Target.RX,s.Input)+"   |   "+RightAxis(Target.RY,s.Input);
        calibrationReadout.Text=$"LX {preview.LX}    LY {preview.LY}    RX {preview.RX}    RY {preview.RY}    LT {preview.LT}/255    RT {preview.RT}/255";
        if(++ticks%5==0)
        {
            foreach(ListViewItem row in mappingList.Items)
                if(row.Tag is Target target&&editing?.Bindings.TryGetValue(target,out var b)==true)row.SubItems[5].Text=MappingService.Read(s.Input,b,target is Target.LT or Target.RT).ToString("F3");
            if(currentPage==3)diagnostic.Text=DiagnosticText(s);
        }
        if(s.Error!=lastLogError&&s.Error.Length>0)
        {
            lastLogError=s.Error;
            try{var log=Path.Combine(store.Root,"errors.log");if(File.Exists(log)&&new FileInfo(log).Length>256000)File.Move(log,log+".previous",true);File.AppendAllText(log,DateTime.UtcNow.ToString("O")+" "+s.Error.Replace(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"%USERPROFILE%",StringComparison.OrdinalIgnoreCase)+Environment.NewLine);}
            catch(IOException){}
        }
    }
    private string RightAxis(Target target, PhysicalState s)
    {
        if (editing?.DeviceKey != s.Device?.Key || editing?.Bindings.TryGetValue(target, out var b) != true) return target + " sem associação";
        if (b is null) return target + " sem associação";
        int raw = b.Kind == InputKind.Axis && b.Index < s.Axes.Length ? s.Axes[b.Index] : b.Kind == InputKind.SdlAxis && b.Index < s.SdlAxes.Length ? s.SdlAxes[b.Index] : 0;
        return $"{target} ← {b} = {raw} → {MappingService.Read(s,b):F3}";
    }
    private string DiagnosticText(EngineSnapshot s) => $"UGA 1.0.0 | .NET {Environment.Version}\r\n{driver}\r\n{s.Status}\r\nSDL3: leitura em thread dedicada, eventos desativados\r\nTaxa medida: {s.ActualHz:F1} Hz (alvo {settings.PollHz})\r\n" +
        $"Controle: {s.Input.Device?.Name ?? "ausente"}\r\nGUID: {s.Input.Device?.Guid}\r\nInstância HID (local): {s.Input.Device?.InstancePath}\r\nConexão: {s.Input.Device?.Connection}\r\n" +
        "Eixos brutos: " + string.Join(" ", s.Input.Axes.Select((v,i) => $"[{i}]={v}")) + "\r\nEixos SDL: " + string.Join(" ", s.Input.SdlAxes) +
        "\r\nBotões: " + string.Join(" ", s.Input.Buttons.Select((v,i) => v ? i.ToString() : "").Where(x => x.Length > 0)) + "\r\nHats: " + string.Join(" ", s.Input.Hats) +
        $"\r\nSaída: {s.Output}\r\n{rightSource.Text}\r\nConflitos: {string.Join(", ", conflicts)}\r\nErro: {s.Error}\r\n{store.LastRecovery}\r\n";
    private void LoadProfileControls()
    {
        if (editing is null) return; deadzone.Value = (decimal)(editing.Deadzone * 100);
        for (int i = 0; i < 4; i++) inversions[i].Checked = editing.Bindings.GetValueOrDefault(Target.LX + i)?.Invert == true;
    }
    private void RefreshMappingRows()
    {
        mappingList.Items.Clear();
        foreach (var target in Enum.GetValues<Target>())
        {
            var b = editing?.Bindings.GetValueOrDefault(target); var row = new ListViewItem(target.ToString()) { Tag = target };
            row.BackColor=row.Index%2==0?Theme.Surface:Theme.SurfaceAlt;row.ForeColor=Theme.Text;
            row.SubItems.AddRange([b?.ToString() ?? "Não associado", b?.Center.ToString() ?? "", b?.NegativeEnd.ToString() ?? "", b?.PositiveEnd.ToString() ?? "", "0"]); mappingList.Items.Add(row);
        }
        mapStatus.Text = editing is null ? "Conecte um controle." : editing.Complete ? "Mapeamento pronto. Salve para aplicar ao controle." : "Associe todos os comandos para habilitar a emulação.";
    }
    private void RunWizard(bool complete)
    {
        if (editing is null || engine.Snapshot.Input.Device?.Key != editing.DeviceKey) { MessageBox.Show(this, "Conecte o controle para mapear.", "UGA"); return; }
        Target? selected = mappingList.SelectedItems.Count > 0 ? (Target)mappingList.SelectedItems[0].Tag! : null;
        if (!complete && selected is null) { mapStatus.Text = "Selecione um comando na lista."; return; }
        var profile = complete ? new MappingProfile { DeviceKey = editing.DeviceKey, Name = "Mapeamento manual", Deadzone = editing.Deadzone } : editing.Copy();
        if (!complete && profile.Bindings.Values.Any(x => x.Kind is InputKind.SdlAxis or InputKind.SdlButton))
        { MessageBox.Show(this, "Para evitar sobreposição entre entradas SDL e brutas, o primeiro mapeamento manual inicia um mapeamento vazio. Complete todos os comandos antes de salvar.", "UGA"); profile.Bindings.Clear(); }
        engine.Suspend(true);
        using var wizard = new MappingWizard(() => engine.Snapshot, profile, complete ? null : selected);
        if (wizard.ShowDialog(this) == DialogResult.OK) { editing = wizard.Profile; LoadProfileControls(); RefreshMappingRows(); }
    }
    private void ClearSelected() { if (mappingList.SelectedItems.Count > 0 && editing is not null) { editing.Bindings.Remove((Target)mappingList.SelectedItems[0].Tag!); RefreshMappingRows(); } }
    private void RestoreMapping()
    {
        var d = engine.Snapshot.Input.Device; if (d is null) return;
        editing = d.HasSdlMapping ? MappingService.SdlDefault(d) : new() { DeviceKey = d.Key }; LoadProfileControls(); RefreshMappingRows();
    }
    private void SaveProfile()
    {
        if (editing is null) return;
        try { editing.Validate(); store.Save(editing); engine.ApplyProfile(editing); mapStatus.Text = "Mapeamento salvo. " + (editing.Complete ? "Volte à Visão geral para iniciar." : "Continue o assistente para completar."); }
        catch (Exception ex) { Error(ex); }
    }
    private async Task RefreshEnvironment()
    {
        try { (driver, conflicts) = await Task.Run(() => { using var b = new ViGEmBackend(); return (b.Probe(), DiagnosticService.FindConflicts()); }); driverStatus.Text = driver; }
        catch (Exception ex) { Error(ex); }
    }
    private void ExportReport()
    {
        using var save = new SaveFileDialog { Filter = "Relatório JSON|*.json", FileName = "UGA-diagnostico.json" };
        if (save.ShowDialog(this) == DialogResult.OK) try { File.WriteAllText(save.FileName, DiagnosticService.Export(engine.Snapshot, driver, conflicts)); } catch (Exception ex) { Error(ex); }
    }
    private async Task ConfigureHidHide()
    {
        var d = engine.Snapshot.Input.Device;
        if (d is null) { ShowRecovery(); return; }
        if (MessageBox.Show(this, $"Controle físico: {d.Name}\nVID {d.Vendor:X4} PID {d.Product:X4}\nHID: {d.InstancePath}\n\nO UGA será permitido antes de ocultar somente este HID. Reconecte o Bluetooth depois.\nSerá salvo um backup local. Aplicar?", "HidHide — consentimento", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;
        engine.RequestEmulation(false);
        try { Enabled = false; await hidHide.Apply(d, Environment.ProcessPath!); MessageBox.Show(this, "Configuração aplicada. Reconecte o controle. Se ficar invisível ao UGA, use Desfazer.", "HidHide"); }
        catch (Exception ex) { Error(ex); } finally { Enabled = true; }
    }
    private async Task UndoHidHide()
    {
        try { Enabled = false; await hidHide.Undo(); MessageBox.Show(this, "Alterações do UGA desfeitas. Reconecte o controle.", "HidHide"); }
        catch (Exception ex) { Error(ex); } finally { Enabled = true; }
    }
    private void ShowRecovery() => MessageBox.Show(this, "1. Instale HidHide pelo botão do fornecedor oficial.\n2. Abra HidHide Configuration Client e adicione o executável REAL do UGA na aba Applications.\n3. Em Devices, identifique o controle físico selecionado pelo VID/PID e caminho HID do diagnóstico. Nunca selecione o Xbox 360 virtual.\n4. Ative a ocultação e reconecte o controle. Preserve entradas de outros aplicativos.\n\nRecuperação: use Desfazer somente alterações UGA. Se o UGA não enxergar o controle, abra o cliente oficial e desmarque SOMENTE o HID físico que você ocultou. Reconecte ou reinicie. Não remova drivers compartilhados.\n\nBackup: %LocalAppData%\\UniversalGamepadAdapter\\hidhide-changes.json", "HidHide — configuração manual e recuperação");
    private void Error(Exception ex) => MessageBox.Show(this, ex.Message, "UGA", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    private void Open(string target) { try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); } catch (Exception ex) { Error(ex); } }
    private sealed record DeviceChoice(string Key, string Name) { public override string ToString() => Name; }
}
