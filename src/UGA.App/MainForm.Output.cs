using UGA.Core;

namespace UGA.App;

internal sealed partial class MainForm
{
    private readonly CheckBox customLed = new() { Text = "Aplicar minha cor ao conectar o controle", AutoSize = true };
    private readonly NumericUpDown vibration = new() { Minimum = 0, Maximum = 100, Width = 85 };
    private readonly Label outputSupport = Theme.MutedLabel(), effectsStatus = Theme.MutedLabel();
    private ModernButton ledChoice = null!, vibrationTest = null!, vibrationStop = null!;
    private int selectedLedColor;

    private void BuildOutputSettings(FlowLayoutPanel content)
    {
        selectedLedColor = settings.LedColor;
        customLed.Checked = settings.CustomLedEnabled;
        vibration.Value = settings.VibrationPercent;
        outputSupport.MaximumSize = effectsStatus.MaximumSize = new(650, 0);
        content.Controls.Add(outputSupport);
        customLed.Margin = new Padding(0, 12, 0, 2);
        content.Controls.Add(customLed);
        ledChoice = Theme.Button("", () =>
        {
            using var dialog = new ColorDialog { Color = Color.FromArgb(unchecked((int)0xFF000000) | selectedLedColor), FullOpen = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            selectedLedColor = dialog.Color.ToArgb() & 0xFFFFFF;
            UpdateLedChoice();
        });
        UpdateLedChoice();
        content.Controls.Add(Buttons(ledChoice));
        content.Controls.Add(Buttons(Theme.Label("Intensidade da vibração"), vibration, Theme.MutedLabel("%  •  0 desativa")));
        var hint = Theme.MutedLabel("A intensidade vale para os jogos e para o teste de 0,5 segundo.\nLED e vibração dependem do suporte do controle e da conexão.");
        hint.MaximumSize = new(650, 0); content.Controls.Add(hint);
        vibrationTest = Theme.Button("Testar vibração", () =>
        {
            if (SaveOutputSettings() && engine.Snapshot.Input.Device is { } device) engine.TestVibration(device.Key);
        });
        vibrationStop = Theme.Button("Parar vibração", engine.StopVibration, ButtonTone.Danger);
        content.Controls.Add(Buttons(Theme.Button("Aplicar e salvar", () => SaveOutputSettings(), ButtonTone.Primary), vibrationTest, vibrationStop));
        effectsStatus.Margin = new Padding(0, 10, 0, 0); content.Controls.Add(effectsStatus);
    }
    private void UpdateLedChoice() => ledChoice.Text = $"Escolher cor  •  #{selectedLedColor:X6}";
    private bool SaveOutputSettings()
    {
        try
        {
            var updated = settings with { CustomLedEnabled = customLed.Checked, LedColor = selectedLedColor, VibrationPercent = (int)vibration.Value };
            store.SaveSettings(updated); settings = updated; engine.Configure(settings);
            return true;
        }
        catch (Exception ex) { Error(ex); return false; }
    }
    private void UpdateOutputControls(EngineSnapshot snapshot)
    {
        var caps = snapshot.OutputCapabilities;
        outputSupport.Text = snapshot.Input.Device is null ? "Conecte um controle para verificar os recursos disponíveis." :
            $"LED RGB: {(caps.RgbLed ? "disponível" : "não suportado nesta conexão")}  •  Vibração: {(caps.Rumble ? "disponível" : "não suportada nesta conexão")}";
        ledChoice.Enabled = caps.RgbLed; customLed.Enabled = caps.RgbLed;
        vibration.Enabled = vibrationTest.Enabled = vibrationStop.Enabled = caps.Rumble;
        effectsStatus.Text = snapshot.OutputStatus.Length > 0 ? snapshot.OutputStatus : "As preferências são salvas neste computador.";
    }
}
