using UGA.Core;

namespace UGA.App;

internal sealed class MappingWizard : Form
{
    private readonly Func<EngineSnapshot> read;
    private readonly Target[] targets;
    private readonly Label instruction = Theme.Label(), live = Theme.Label(), result = Theme.Label();
    private readonly Button next;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 40 };
    private PhysicalState? baseline;
    private InputBinding? candidate;
    private int step, phase; // 0 neutral, 1 positive/button, 2 negative, 3 confirm.
    private int furthest;
    private readonly string deviceKey;
    public MappingProfile Profile { get; }
    public MappingWizard(Func<EngineSnapshot> read, MappingProfile profile, Target? only = null, bool axesOnly = false)
    {
        this.read = read; deviceKey = profile.DeviceKey; Profile = profile.Copy();
        Icon=Branding.CreateIcon();
        targets = axesOnly ? Enum.GetValues<Target>().Where(t => t >= Target.LX).ToArray() : only.HasValue ? [only.Value] : Enum.GetValues<Target>();
        Text = "Assistente de mapeamento — entradas brutas"; Size = new(720, 440); MinimumSize = new(620, 400); StartPosition = FormStartPosition.CenterParent;
        var layout = Theme.Flow(true); Controls.Add(layout);
        layout.Controls.Add(Theme.Label("Mapeamento físico → Xbox", true)); layout.Controls.Add(instruction); layout.Controls.Add(live); layout.Controls.Add(result);
        next = Theme.Button("Capturar neutro", Advance); layout.Controls.Add(next);
        layout.Controls.Add(Theme.Button("Cancelar", () => { DialogResult = DialogResult.Cancel; Close(); }));
        timer.Tick += (_, _) => TickCapture(); timer.Start(); FormClosed += (_, _) => timer.Dispose();
        Theme.Apply(this); Prompt();
    }
    private Target Current => targets[step];
    private void Prompt()
    {
        string name = Current.ToString();
        instruction.Text = $"{step + 1}/{targets.Length} — {name}\n" + (phase switch
        {
            0 => "Solte todos os comandos, deixe os analógicos no centro e os gatilhos soltos.\nMantenha o corpo do controle imóvel. Clique em Capturar neutro.",
            1 when Current < Target.LX => "Pressione apenas o botão solicitado ou a direção do D-pad. Depois clique em Confirmar.",
            1 when Current is Target.LT or Target.RT => "Pressione o gatilho até o fim, mantenha e clique em Registrar extremo.\nGatilhos digitais também são aceitos.",
            1 => "Mova apenas este analógico até o fim: " + (Current is Target.LY or Target.RY ? "PARA CIMA." : "PARA A DIREITA.") + "\nMantenha e clique em Registrar extremo.",
            2 => "Mova o MESMO analógico até o extremo oposto (" + (Current is Target.LY or Target.RY ? "BAIXO" : "ESQUERDA") + ").\nMantenha e clique em Registrar extremo oposto.",
            _ => "Confira a entrada e os limites abaixo. Confirme para continuar."
        });
        next.Text = phase switch { 0 => "Capturar neutro", 1 when Current < Target.LX => "Confirmar botão", 1 => "Registrar extremo", 2 => "Registrar extremo oposto", _ => "Confirmar mapeamento" };
        result.Text = "";
    }
    private void TickCapture()
    {
        var s = read().Input;
        if (s.Device?.Key != deviceKey) { result.Text = "Controle desconectado ou substituído. Reconecte e reinicie esta etapa."; baseline = null; phase = 0; return; }
        live.Text = "Eixos brutos: " + string.Join("  ", s.Axes.Select((v,i) => $"{i}:{v}")) + "\nBotões: " + string.Join(", ", s.Buttons.Select((v,i) => v ? i.ToString() : "").Where(x => x.Length > 0)) + "  Hats: " + string.Join(",", s.Hats);
        if (baseline is null || phase is 0 or 3) return;
        if (phase == 1 && Current >= Target.LX)
        {
            for (int i = 0; i < Math.Min(s.Axes.Length, baseline.Axes.Length); i++)
            {
                int delta = Math.Abs(s.Axes[i] - baseline.Axes[i]);
                if (delta > Math.Max(6000, furthest)) { furthest = delta; candidate = new() { Kind = InputKind.Axis, Index = i, Center = baseline.Axes[i], PositiveEnd = s.Axes[i] }; }
            }
        }
        if (phase == 1 && (Current < Target.LX || Current is Target.LT or Target.RT && candidate?.Kind != InputKind.Axis))
        {
            for (int i = 0; i < Math.Min(s.Buttons.Length, baseline.Buttons.Length); i++)
                if (s.Buttons[i] && !baseline.Buttons[i]) candidate = new() { Kind = InputKind.Button, Index = i };
            if (Current < Target.LX)
                for (int i = 0; i < Math.Min(s.Hats.Length, baseline.Hats.Length); i++)
                    foreach (int mask in new[] { 1,2,4,8 }) if ((s.Hats[i] & mask) != 0 && (baseline.Hats[i] & mask) == 0) candidate = new() { Kind = InputKind.Hat, Index = i, HatMask = mask };
        }
        if (phase == 2 && candidate is { Kind: InputKind.Axis } b && b.Index < s.Axes.Length)
        {
            int value = s.Axes[b.Index];
            if (Math.Sign(value - b.Center) != Math.Sign(b.PositiveEnd - b.Center) && Math.Abs(value - b.Center) > Math.Abs(b.NegativeEnd - b.Center)) candidate = b with { NegativeEnd = value };
        }
        if (candidate is { } binding) result.Text = $"Detectado: {binding}\nNeutro {binding.Center} | negativo {binding.NegativeEnd} | positivo {binding.PositiveEnd}\nNormalizado: {MappingService.Read(s, binding, Current is Target.LT or Target.RT):F3}";
    }
    private void Advance()
    {
        var input = read().Input;
        if (input.Device?.Key != deviceKey) { result.Text = "Conecte o mesmo controle para continuar."; return; }
        if (phase == 0) { baseline = input.Copy(); candidate = null; furthest = 0; phase = 1; Prompt(); return; }
        if (candidate is null) { result.Text = "Nenhuma entrada detectada. Acione somente o comando pedido."; return; }
        if (phase == 1 && Current is Target.LX or Target.LY or Target.RX or Target.RY)
        { candidate = candidate with { NegativeEnd = candidate.Center }; phase = 2; Prompt(); return; }
        if (phase is 1 or 2) { phase = 3; Prompt(); result.Text = $"{candidate}\nNeutro: {candidate.Center} | negativo: {candidate.NegativeEnd} | positivo: {candidate.PositiveEnd}"; return; }
        var previous = Profile.Bindings.GetValueOrDefault(Current);
        Profile.Bindings[Current] = candidate;
        try { Profile.Validate(); }
        catch (InvalidDataException ex)
        {
            if (previous is null) Profile.Bindings.Remove(Current); else Profile.Bindings[Current] = previous;
            phase = 0; Prompt(); result.Text = ex.Message + " Refaça esta etapa. Se a entrada já estiver atribuída, limpe a associação anterior."; return;
        }
        if (++step == targets.Length) { DialogResult = DialogResult.OK; Close(); return; }
        phase = 0; baseline = null; candidate = null; Prompt();
    }
}
