using UGA.Core;
using UGA.Infrastructure.Diagnostics;
using UGA.Infrastructure.HidHide;

namespace UGA.App;

internal sealed partial class MainForm
{
    private readonly Panel[] pages=new Panel[5];
    private readonly ModernButton[] navigation=new ModernButton[5];
    private readonly Label pageTitle=Theme.Label("Visão geral",true),pageSubtitle=Theme.MutedLabel(),
        heroDescription=Theme.MutedLabel(),emulationBadge=Theme.Caption("● EM ESPERA"),outputStatus=Theme.Label(),
        deviceMapping=Theme.MutedLabel(),conflictStatus=Theme.Label(),sidebarState=Theme.Label(),sidebarDetail=Theme.MutedLabel(),calibrationReadout=Theme.MutedLabel();
    private readonly ControllerView outputView=new(),sourceView=new(),calibrationView=new();
    private readonly ComboBox themeChoice=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=200};
    private ModernButton themeSwitch=null!;
    private int currentPage;
    private bool changingTheme;
    private static readonly string[] PageNames=["Visão geral","Mapeamento","Calibração","Diagnóstico","Configurações"];
    private static readonly string[] PageDescriptions=["Monitore seu controle e a emulação em tempo real.","Associe os comandos físicos ao layout Xbox 360.","Ajuste os analógicos e os gatilhos do seu controle.","Confira as entradas, os drivers e os possíveis conflitos.","Deixe o UGA do seu jeito."];
    private static TableLayoutPanel Grid(int columns=1)=>new(){Dock=DockStyle.Fill,ColumnCount=columns,Margin=Padding.Empty,Padding=Padding.Empty};
    private static void Row(TableLayoutPanel grid,float height,SizeType type=SizeType.Absolute)=>grid.RowStyles.Add(new RowStyle(type,height));
    private static FlowLayoutPanel Buttons(params Control[] controls)
    {var f=Theme.Flow();f.AutoSize=true;f.Dock=DockStyle.Top;f.Margin=new Padding(0,12,0,0);f.Controls.AddRange(controls);return f;}
    private static CardPanel Card(string caption,string? heading=null)
    {
        var card=new CardPanel{Dock=DockStyle.Fill};
        var title=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty};
        title.Controls.Add(Theme.Caption(caption));if(heading is not null){var h=Theme.Label(heading);h.Font=new Font("Segoe UI",11,FontStyle.Bold);title.Controls.Add(h);}
        card.Controls.Add(title);return card;
    }
    private void BuildShell()
    {
        var shell=Grid(2);Row(shell,100,SizeType.Percent);shell.ColumnStyles.Add(new(SizeType.Absolute,214));shell.ColumnStyles.Add(new(SizeType.Percent,100));Controls.Add(shell);
        var sidebar=new Panel{Dock=DockStyle.Fill,Padding=new Padding(14,24,14,18),Tag="sidebar"};shell.Controls.Add(sidebar,0,0);
        var brandPanel=new Panel{Dock=DockStyle.Top,Height=102};
        var brand=Theme.Label("UGA",true);brand.Location=new Point(82,8);brand.Font=new Font("Segoe UI",22,FontStyle.Bold);
        var brandSub=Theme.MutedLabel("Universal Gamepad Adapter");brandSub.Location=new Point(10,62);brandSub.Font=new Font("Segoe UI",8);
        brandPanel.Controls.Add(new LogoView{Location=new Point(6,5),Size=new Size(68,48)});brandPanel.Controls.Add(brand);brandPanel.Controls.Add(brandSub);
        var nav=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=1,RowCount=5,Height=250,Padding=new Padding(0,8,0,0)};
        string[] icons=["home","gamepad","sliders","pulse","settings"];
        for(int i=0;i<5;i++){int index=i;navigation[i]=Theme.Button(PageNames[i],()=>Navigate(index),ButtonTone.Navigation,icons[i]);navigation[i].Dock=DockStyle.Fill;navigation[i].AutoSize=false;navigation[i].Margin=new Padding(0,2,0,4);nav.Controls.Add(navigation[i]);}
        var footer=Theme.Flow(true);footer.Dock=DockStyle.Bottom;footer.Height=156;footer.AutoScroll=false;
        sidebarState.Font=new Font("Segoe UI",9,FontStyle.Bold);sidebarDetail.Font=new Font("Segoe UI",8);
        footer.Controls.Add(sidebarState);footer.Controls.Add(sidebarDetail);footer.Controls.Add(Theme.MutedLabel("v1.0  •  Windows x64"));
        var exit=Theme.Button("Sair do UGA",()=>{exitRequested=true;Close();},ButtonTone.Navigation,"exit");exit.AutoSize=false;exit.Width=170;exit.Margin=Padding.Empty;footer.Controls.Add(exit);
        sidebar.Controls.Add(nav);sidebar.Controls.Add(brandPanel);sidebar.Controls.Add(footer);
        var main=Grid();main.Padding=new Padding(28,18,12,12);main.RowCount=2;Row(main,88);Row(main,100,SizeType.Percent);shell.Controls.Add(main,1,0);
        var header=Grid(2);Row(header,100,SizeType.Percent);header.ColumnStyles.Add(new(SizeType.Percent,100));header.ColumnStyles.Add(new(SizeType.Absolute,390));
        var heading=Theme.Flow(true);heading.AutoScroll=false;heading.Controls.Add(pageTitle);heading.Controls.Add(pageSubtitle);header.Controls.Add(heading,0,0);
        heading.Resize+=(_,_)=>pageSubtitle.MaximumSize=new Size(Math.Max(100,heading.ClientSize.Width-8),0);
        var headerActions=Buttons(deviceSelect,themeSwitch=Theme.Button("Modo claro",ToggleTheme,ButtonTone.Neutral,"sun"));headerActions.Dock=DockStyle.Fill;headerActions.Margin=new Padding(0,8,0,0);headerActions.AutoSize=false;headerActions.WrapContents=false;headerActions.AutoScroll=false;
        themeSwitch.AutoSize=false;themeSwitch.Width=155;
        deviceSelect.Width=195;deviceSelect.Margin=new Padding(0,5,12,0);deviceSelect.AccessibleName="Selecionar controle físico";header.Controls.Add(headerActions,1,0);main.Controls.Add(header,0,0);
        var content=new Panel{Dock=DockStyle.Fill};main.Controls.Add(content,0,1);
        for(int i=0;i<5;i++){pages[i]=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Visible=false};content.Controls.Add(pages[i]);}
        BuildOverview();BuildMapping();BuildCalibration();BuildDiagnostics();BuildSettings();
        foreach(var combo in new[]{deviceSelect,themeChoice})
        {
            combo.DrawMode=DrawMode.OwnerDrawFixed;combo.ItemHeight=25;
            combo.DrawItem+=(_,e)=>{if(e.Index<0)return;using var fill=new SolidBrush((e.State&DrawItemState.Selected)!=0?Theme.BlueTint:Theme.SurfaceAlt);e.Graphics.FillRectangle(fill,e.Bounds);TextRenderer.DrawText(e.Graphics,combo.Items[e.Index]?.ToString(),combo.Font,Rectangle.Inflate(e.Bounds,-6,0),Theme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};
        }
    }
    private void Navigate(int index)
    {
        currentPage=index;
        for(int i=0;i<pages.Length;i++){pages[i].Visible=i==index;navigation[i].Selected=i==index;navigation[i].Invalidate();}
        pages[index].BringToFront();pageTitle.Text=PageNames[index];pageSubtitle.Text=PageDescriptions[index];
        engine.Suspend(index is 1 or 2);
    }
    private void BuildOverview()
    {
        var dashboard=Grid(2);dashboard.Dock=DockStyle.Top;dashboard.Height=610;dashboard.ColumnStyles.Add(new(SizeType.Percent,62));dashboard.ColumnStyles.Add(new(SizeType.Percent,38));
        Row(dashboard,310);Row(dashboard,238);Row(dashboard,62);pages[0].Controls.Add(dashboard);
        var hero=new CardPanel{Dock=DockStyle.Fill,Hero=true};dashboard.Controls.Add(hero,0,0);
        var heroGrid=Grid();Row(heroGrid,26);Row(heroGrid,45);Row(heroGrid,46);Row(heroGrid,100,SizeType.Percent);Row(heroGrid,55);hero.Controls.Add(heroGrid);
        var heroHead=Grid(2);heroHead.ColumnStyles.Add(new(SizeType.Percent,100));heroHead.ColumnStyles.Add(new(SizeType.AutoSize));heroHead.Controls.Add(Theme.Caption("EMULAÇÃO XBOX 360"),0,0);heroHead.Controls.Add(emulationBadge,1,0);heroGrid.Controls.Add(heroHead,0,0);
        virtualStatus.Font=new Font("Segoe UI",20,FontStyle.Bold);heroGrid.Controls.Add(virtualStatus,0,1);heroGrid.Controls.Add(heroDescription,0,2);
        outputView.MinimumSize=new(120,65);heroGrid.Controls.Add(outputView,0,3);
        var bottom=Grid(3);Row(bottom,100,SizeType.Percent);bottom.ColumnStyles.Add(new(SizeType.Percent,100));bottom.ColumnStyles.Add(new(SizeType.Absolute,72));bottom.ColumnStyles.Add(new(SizeType.Absolute,180));
        var virtualLabels=Theme.Flow(true);virtualLabels.AutoScroll=false;virtualLabels.Controls.Add(Theme.Caption("DISPOSITIVO VIRTUAL"));outputStatus.Font=new Font("Segoe UI",9,FontStyle.Bold);virtualLabels.Controls.Add(outputStatus);bottom.Controls.Add(virtualLabels,0,0);
        var hz=Theme.Flow(true);hz.AutoScroll=false;hz.Controls.Add(Theme.Caption("LEITURA"));activity.Font=new Font("Segoe UI",10,FontStyle.Bold);hz.Controls.Add(activity);bottom.Controls.Add(hz,1,0);toggle.AutoSize=false;toggle.Dock=DockStyle.Fill;toggle.Margin=new Padding(0,10,0,2);bottom.Controls.Add(toggle,2,0);heroGrid.Controls.Add(bottom,0,4);
        var connected=new CardPanel{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,16)};dashboard.Controls.Add(connected,1,0);
        var connectedGrid=Grid();Row(connectedGrid,25);Row(connectedGrid,30);Row(connectedGrid,100,SizeType.Percent);Row(connectedGrid,28);Row(connectedGrid,23);connected.Controls.Add(connectedGrid);
        connectedGrid.Controls.Add(Theme.Caption("CONTROLE CONECTADO"),0,0);physical.Font=new Font("Segoe UI",11,FontStyle.Bold);connectedGrid.Controls.Add(physical,0,1);connectedGrid.Controls.Add(mainView,0,2);connectedGrid.Controls.Add(connection,0,3);connectedGrid.Controls.Add(deviceMapping,0,4);deviceMapping.Font=new Font("Segoe UI",8.5f);
        var quick=new CardPanel{Dock=DockStyle.Fill};dashboard.Controls.Add(quick,0,1);var quickGrid=Grid();Row(quickGrid,27);Row(quickGrid,54);Row(quickGrid,54);Row(quickGrid,54);quick.Controls.Add(quickGrid);quickGrid.Controls.Add(Theme.Caption("ACESSO RÁPIDO  /  AJUSTES ESSENCIAIS"),0,0);
        string[] titles=["Mapear botões","Calibrar analógicos","Executar diagnóstico"],descriptions=["Personalize os comandos do seu controle","Ajuste a zona morta e a direção dos eixos","Verifique os drivers e as entradas"],icons=["gamepad","sliders","pulse"];
        for(int i=0;i<3;i++){int target=i+1;var action=Theme.Button(titles[i],()=>Navigate(target),ButtonTone.Quick,icons[i]);action.Description=descriptions[i];action.Dock=DockStyle.Fill;action.AutoSize=false;action.Margin=Padding.Empty;quickGrid.Controls.Add(action,0,i+1);}
        var health=new CardPanel{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,16)};dashboard.Controls.Add(health,1,1);var healthGrid=Grid();Row(healthGrid,28);Row(healthGrid,56);Row(healthGrid,56);Row(healthGrid,56);health.Controls.Add(healthGrid);healthGrid.Controls.Add(Theme.Caption("SAÚDE DO SISTEMA"),0,0);
        void HealthRow(int row,string title,Label value){var f=Theme.Flow(true);f.AutoScroll=false;var t=Theme.Label(title);t.Font=new Font("Segoe UI",9,FontStyle.Bold);f.Controls.Add(t);value.Font=new Font("Segoe UI",8.5f);f.Controls.Add(value);healthGrid.Controls.Add(f,0,row);}
        HealthRow(1,"Driver virtual",driverStatus);HealthRow(2,"HidHide",hidStatus);HealthRow(3,"Outros remapeadores",conflictStatus);
        notice.MaximumSize=new Size(1000,0);notice.Font=new Font("Segoe UI",9);dashboard.Controls.Add(notice,0,2);dashboard.SetColumnSpan(notice,2);
        dashboard.Resize+=(_,_)=>{heroDescription.MaximumSize=new Size(Math.Max(150,hero.ClientSize.Width-48),0);notice.MaximumSize=new Size(Math.Max(100,dashboard.ClientSize.Width-18),0);};
    }
    private void BuildMapping()
    {
        var layout=Grid();Row(layout,235);Row(layout,100,SizeType.Percent);Row(layout,75);pages[1].Controls.Add(layout);
        var visuals=Grid(2);Row(visuals,100,SizeType.Percent);visuals.ColumnStyles.Add(new(SizeType.Percent,50));visuals.ColumnStyles.Add(new(SizeType.Percent,50));layout.Controls.Add(visuals,0,0);
        var inputCard=Card("ENTRADA FÍSICA");var outputCard=Card("LAYOUT DE SAÍDA  /  XBOX 360");outputCard.Margin=new Padding(0,0,0,16);
        sourceView.Dock=DockStyle.Fill;mapView.Dock=DockStyle.Fill;inputCard.Controls.Add(sourceView);sourceView.BringToFront();sourceView.Padding=new Padding(0,25,0,0);outputCard.Controls.Add(mapView);mapView.BringToFront();
        visuals.Controls.Add(inputCard,0,0);visuals.Controls.Add(outputCard,1,0);
        var listCard=new CardPanel{Dock=DockStyle.Fill,Padding=new Padding(16),Margin=new Padding(0,0,0,12)};layout.Controls.Add(listCard,0,1);listCard.Controls.Add(mappingList);
        foreach(var (title,width) in new[]{("Xbox",65),("Entrada física",195),("Centro",80),("Mínimo",80),("Máximo",80),("Valor",75)})mappingList.Columns.Add(title,width);
        mappingList.OwnerDraw=true;mappingList.HeaderStyle=ColumnHeaderStyle.Nonclickable;
        mappingList.DrawColumnHeader+=(_,e)=>{using var b=new SolidBrush(Theme.SurfaceAlt);e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,e.Header?.Text,mappingList.Font,Rectangle.Inflate(e.Bounds,-6,0),Theme.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.Left);};
        mappingList.DrawItem+=(_,e)=>e.DrawDefault=false;
        mappingList.DrawSubItem+=(_,e)=>{if(e.Item is null||e.SubItem is null)return;Color bg=e.Item.Selected?Theme.BlueTint:e.ItemIndex%2==0?Theme.Surface:Theme.SurfaceAlt;using var b=new SolidBrush(bg);e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,e.SubItem.Text,mappingList.Font,Rectangle.Inflate(e.Bounds,-6,0),e.ColumnIndex==0?Theme.Blue:Theme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};
        mappingList.Resize+=(_,_)=>{if(mappingList.Columns.Count>1)mappingList.Columns[1].Width=Math.Max(145,mappingList.ClientSize.Width-380);};
        var footer=Theme.Flow(true);footer.AutoScroll=false;
        footer.Controls.Add(Buttons(Theme.Button("Assistente completo",()=>RunWizard(true),ButtonTone.Primary,"gamepad"),Theme.Button("Mapear selecionado",()=>RunWizard(false)),Theme.Button("Limpar",ClearSelected),Theme.Button("Restaurar",RestoreMapping),Theme.Button("Salvar mapeamento",SaveProfile)));
        mapStatus.Font=new Font("Segoe UI",8.5f);footer.Controls.Add(mapStatus);layout.Controls.Add(footer,0,2);
    }
    private void BuildCalibration()
    {
        var layout=Grid(2);Row(layout,100,SizeType.Percent);layout.ColumnStyles.Add(new(SizeType.Percent,56));layout.ColumnStyles.Add(new(SizeType.Percent,44));pages[2].Controls.Add(layout);
        var visual=new CardPanel{Dock=DockStyle.Fill};var right=new CardPanel{Dock=DockStyle.Fill,Margin=Padding.Empty};layout.Controls.Add(visual,0,0);layout.Controls.Add(right,1,0);
        var left=Grid();Row(left,28);Row(left,100,SizeType.Percent);Row(left,60);Row(left,58);visual.Controls.Add(left);
        left.Controls.Add(Theme.Caption("RESPOSTA DOS ANALÓGICOS E GATILHOS"),0,0);left.Controls.Add(calibrationView,0,1);calibrationReadout.MaximumSize=new(500,0);left.Controls.Add(calibrationReadout,0,2);rightSource.MaximumSize=new(500,0);rightSource.Font=new Font("Segoe UI",8.5f);left.Controls.Add(rightSource,0,3);
        var controls=Theme.Flow(true);right.Controls.Add(controls);controls.Controls.Add(Theme.Caption("AJUSTES DO CONTROLE"));controls.Controls.Add(Theme.Label("Zona morta",true));
        var info=Theme.MutedLabel("Ignora pequenas variações do analógico em repouso.\nRecomendado para começar: 8%.");info.MaximumSize=new(310,0);controls.Controls.Add(info);
        deadzone.Margin=new Padding(0,12,0,8);controls.Controls.Add(Buttons(deadzone,Theme.Label("%  •  intervalo de 0 a 30%")));
        controls.Controls.Add(Theme.Caption("DIREÇÃO DOS EIXOS"));foreach(var inversion in inversions){inversion.Margin=new Padding(0,8,0,4);controls.Controls.Add(inversion);}
        controls.Controls.Add(Buttons(Theme.Button("Aplicar ajustes",SaveAllSettings,ButtonTone.Primary,"check")));
        controls.Controls.Add(Theme.Caption("CALIBRAÇÃO MANUAL"));var explanation=Theme.MutedLabel("Capture o neutro e os dois extremos dos analógicos,\nalém do repouso e do fim dos gatilhos.");explanation.MaximumSize=new(310,0);controls.Controls.Add(explanation);
        controls.Controls.Add(Buttons(Theme.Button("Calibrar eixos e gatilhos",RunCalibrationWizard,ButtonTone.Neutral,"sliders")));
    }
    private void BuildDiagnostics()
    {
        var card=new CardPanel{Dock=DockStyle.Fill,Margin=Padding.Empty};pages[3].Controls.Add(card);
        var layout=Grid();Row(layout,28);Row(layout,100,SizeType.Percent);Row(layout,65);card.Controls.Add(layout);
        layout.Controls.Add(Theme.Caption("DADOS EM TEMPO REAL  /  DIAGNÓSTICO LOCAL"),0,0);diagnostic.Font=new Font("Consolas",9.5f);layout.Controls.Add(diagnostic,0,1);
        layout.Controls.Add(Buttons(Theme.Button("Exportar relatório",ExportReport,ButtonTone.Primary),Theme.Button("Controles do Windows",()=>Open("joy.cpl")),Theme.Button("Verificar novamente",()=>_=RefreshEnvironment(),ButtonTone.Neutral,"pulse")),0,2);
    }
    private void BuildSettings()
    {
        var flow=Theme.Flow(true);flow.WrapContents=false;pages[4].Controls.Add(flow);
        CardPanel Section(string caption,string title,int height)
        {
            var card=new CardPanel{Width=760,Height=height,Margin=new Padding(0,0,0,16)};var content=Theme.Flow(true);content.AutoScroll=false;card.Controls.Add(content);content.Controls.Add(Theme.Caption(caption));var h=Theme.Label(title);h.Font=new Font("Segoe UI",12,FontStyle.Bold);content.Controls.Add(h);flow.Controls.Add(card);return card;
        }
        var appearance=Section("APARÊNCIA","Escolha seu tema",156);var a=(FlowLayoutPanel)appearance.Controls[0];a.Controls.Add(Theme.MutedLabel("O tema é aplicado imediatamente e salvo neste computador."));
        themeChoice.Items.AddRange(["Escuro","Claro"]);themeChoice.Margin=new Padding(0,10,0,0);a.Controls.Add(themeChoice);themeChoice.SelectedIndexChanged+=(_,_)=>{if(!changingTheme)SetTheme(themeChoice.SelectedIndex==1?"Light":"Dark");};
        var effects=Section("CONTROLE FÍSICO","LED e vibração",410);
        BuildOutputSettings((FlowLayoutPanel)effects.Controls[0]);
        var app=Section("APLICATIVO","Inicialização e processamento",350);var content=(FlowLayoutPanel)app.Controls[0];
        foreach(var option in new[]{autoStart,trayOption,startup}){option.Margin=new Padding(0,10,0,3);content.Controls.Add(option);}
        content.Controls.Add(Buttons(Theme.MutedLabel("Frequência alvo"),frequency,Theme.MutedLabel("Hz  •  125 a 250")));
        content.Controls.Add(Buttons(Theme.Button("Salvar preferências",SaveAllSettings,ButtonTone.Primary),Theme.Button("Restaurar padrões",ResetSettings)));
        var drivers=Section("DRIVERS E ENTRADAS DUPLICADAS","Configure seus drivers",310);var d=(FlowLayoutPanel)drivers.Controls[0];
        d.Controls.Add(Theme.MutedLabel("Use as distribuições oficiais. HidHide é opcional."));
        d.Controls.Add(Buttons(Theme.Button("Fornecedor ViGEmBus",()=>Open(DiagnosticService.ViGEmDownload)),Theme.Button("Fornecedor HidHide",()=>Open(HidHideService.OfficialDownload))));
        d.Controls.Add(Buttons(Theme.Button("Configurar HidHide",()=>_=ConfigureHidHide(),ButtonTone.Primary,"shield"),Theme.Button("Desfazer alterações UGA",()=>_=UndoHidHide())));
        d.Controls.Add(Buttons(Theme.Button("Instruções e recuperação",ShowRecovery)));
        flow.Resize+=(_,_)=>{foreach(Control child in flow.Controls)child.Width=Math.Max(400,flow.ClientSize.Width-25);};
    }
    private void ToggleTheme()=>SetTheme(Theme.IsLight?"Dark":"Light");
    private void SetTheme(string theme)
    {
        settings=settings with{Theme=theme};Theme.Configure(theme);Theme.Apply(this);UpdateThemeControls();SaveSettings();UpdateUi();
    }
    private void UpdateThemeControls()
    {
        changingTheme=true;themeChoice.SelectedIndex=Theme.IsLight?1:0;changingTheme=false;
        themeSwitch.Text=Theme.IsLight?"Modo escuro":"Modo claro";themeSwitch.Glyph=Theme.IsLight?"moon":"sun";themeSwitch.Invalidate();
    }
    private void RunCalibrationWizard()
    {
        if(editing is null||engine.Snapshot.Input.Device?.Key!=editing.DeviceKey){MessageBox.Show(this,"Conecte seu controle antes de calibrar.","UGA");return;}
        engine.Suspend(true);var mapping=editing.Copy();foreach(var t in Enum.GetValues<Target>().Where(t=>t>=Target.LX))mapping.Bindings.Remove(t);
        using var wizard=new MappingWizard(()=>engine.Snapshot,mapping,null,true);
        if(wizard.ShowDialog(this)==DialogResult.OK){editing=wizard.Profile;LoadProfileControls();RefreshMappingRows();SaveProfile();}
    }
}
