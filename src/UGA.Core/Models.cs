using System.Text.Json;
using System.Text.Json.Serialization;

namespace UGA.Core;

public enum Target { A, B, X, Y, LB, RB, Back, Start, L3, R3, Up, Down, Left, Right, LX, LY, RX, RY, LT, RT }
public enum InputKind { SdlButton, SdlAxis, Button, Axis, Hat }
public sealed record InputBinding
{
    public InputKind Kind { get; init; }
    public int Index { get; init; }
    public int HatMask { get; init; }
    public int Center { get; init; }
    public int NegativeEnd { get; init; } = -32768;
    public int PositiveEnd { get; init; } = 32767;
    public bool Invert { get; init; }
    public override string ToString() => $"{Kind}[{Index}]" + (Kind == InputKind.Hat ? $" máscara {HatMask}" : "") + (Invert ? " invertido" : "");
}

public sealed record DeviceInfo(uint InstanceId, string Name, string Key, string Guid, ushort Vendor, ushort Product,
    string Path, string InstancePath, string Connection, bool HasSdlMapping);

// Reused by the reader; only copies published to the UI are immutable by convention.
public sealed class PhysicalState
{
    public DeviceInfo? Device { get; set; }
    public short[] Axes { get; set; } = [];
    public bool[] Buttons { get; set; } = [];
    public byte[] Hats { get; set; } = [];
    public short[] SdlAxes { get; set; } = new short[6];
    public bool[] SdlButtons { get; set; } = new bool[26];
    public PhysicalState Copy() => new() { Device = Device, Axes = (short[])Axes.Clone(), Buttons = (bool[])Buttons.Clone(),
        Hats = (byte[])Hats.Clone(), SdlAxes = (short[])SdlAxes.Clone(), SdlButtons = (bool[])SdlButtons.Clone() };
    public void Clear() { Device = null; Array.Clear(Axes); Array.Clear(Buttons); Array.Clear(Hats); Array.Clear(SdlAxes); Array.Clear(SdlButtons); }
}

[Flags]
public enum PadButtons : ushort
{
    None = 0, Up = 1, Down = 2, Left = 4, Right = 8, Start = 16, Back = 32,
    L3 = 64, R3 = 128, LB = 256, RB = 512, A = 4096, B = 8192, X = 16384, Y = 32768
}
public readonly record struct VirtualState(PadButtons Buttons, short LX, short LY, short RX, short RY, byte LT, byte RT)
{
    public static VirtualState Neutral => default;
}

public sealed record MappingProfile
{
    public int Version { get; init; } = 1;
    public string DeviceKey { get; init; } = "";
    public string Name { get; init; } = "Perfil";
    public float Deadzone { get; init; } = .08f;
    public Dictionary<Target, InputBinding> Bindings { get; init; } = [];
    [JsonIgnore] public bool Complete => Enum.GetValues<Target>().All(Bindings.ContainsKey);
    public MappingProfile Copy() => this with { Bindings = new(Bindings) };
    public void Validate()
    {
        if (Version != 1 || string.IsNullOrWhiteSpace(DeviceKey) || !float.IsFinite(Deadzone) || Deadzone is < 0 or > .30f)
            throw new InvalidDataException("Perfil inválido: versão, identificação ou deadzone.");
        var used = new HashSet<(InputKind, int, int)>();
        foreach (var (target, b) in Bindings)
        {
            if (b is null || !Enum.IsDefined(target) || !Enum.IsDefined(b.Kind) || b.Index is < 0 or > 255 ||
                (b.Kind == InputKind.SdlAxis && b.Index > 5) || (b.Kind == InputKind.SdlButton && b.Index > 25))
                throw new InvalidDataException("Índice de entrada inválido.");
            if (b.Kind == InputKind.Hat && b.HatMask is not (1 or 2 or 4 or 8)) throw new InvalidDataException("Hat inválido.");
            if (b.Center is < -32768 or > 32767 || b.NegativeEnd is < -32768 or > 32767 || b.PositiveEnd is < -32768 or > 32767)
                throw new InvalidDataException("Limites de calibração inválidos.");
            if (b.Kind is InputKind.Axis or InputKind.SdlAxis)
            {
                if (Math.Abs(b.PositiveEnd - b.Center) < 1024 || (target is Target.LX or Target.LY or Target.RX or Target.RY &&
                    (Math.Abs(b.NegativeEnd - b.Center) < 1024 || Math.Sign(b.PositiveEnd - b.Center) == Math.Sign(b.NegativeEnd - b.Center))))
                    throw new InvalidDataException("Calibre o centro e os dois extremos do eixo.");
            }
            if (target >= Target.LX && target <= Target.RY && b.Kind is not (InputKind.Axis or InputKind.SdlAxis))
                throw new InvalidDataException("Analógicos exigem eixos.");
            if (target < Target.LX && b.Kind is InputKind.Axis or InputKind.SdlAxis)
                throw new InvalidDataException("Botões exigem botão ou hat.");
            if (!used.Add((b.Kind, b.Index, b.Kind == InputKind.Hat ? b.HatMask : 0))) throw new InvalidDataException("Entrada atribuída a dois comandos.");
        }
    }
}

public sealed record AppSettings
{
    public string Theme { get; init; } = "Dark";
    public bool AutoStart { get; init; }
    public bool MinimizeToTray { get; init; } = true;
    public bool StartWithWindows { get; init; }
    public int PollHz { get; init; } = 125;
    public string PreferredDeviceKey { get; init; } = "";
    public bool CustomLedEnabled { get; init; }
    public int LedColor { get; init; } = 0x2583FF;
    public int VibrationPercent { get; init; } = 100;
    public void Validate()
    {
        if (PollHz is < 125 or > 250) throw new InvalidDataException("Frequência deve ficar entre 125 e 250 Hz.");
        if (Theme is not ("Dark" or "Light")) throw new InvalidDataException("Tema inválido.");
        if (LedColor is < 0 or > 0xFFFFFF || VibrationPercent is < 0 or > 100)
            throw new InvalidDataException("Cor ou intensidade da vibração inválida.");
    }
}

public static class JsonFormat
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true,
        Converters = { new JsonStringEnumConverter() } };
}
