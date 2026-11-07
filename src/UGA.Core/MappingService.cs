namespace UGA.Core;

public sealed class MappingService : IMappingService
{
    private static readonly (Target Target, PadButtons Button)[] ButtonMap =
    [ (Target.A, PadButtons.A), (Target.B, PadButtons.B), (Target.X, PadButtons.X), (Target.Y, PadButtons.Y),
      (Target.LB, PadButtons.LB), (Target.RB, PadButtons.RB), (Target.Back, PadButtons.Back), (Target.Start, PadButtons.Start),
      (Target.L3, PadButtons.L3), (Target.R3, PadButtons.R3), (Target.Up, PadButtons.Up), (Target.Down, PadButtons.Down),
      (Target.Left, PadButtons.Left), (Target.Right, PadButtons.Right) ];

    public VirtualState Map(PhysicalState input, MappingProfile profile)
    {
        if (input.Device is null || input.Device.Key != profile.DeviceKey) return default;
        var buttons = PadButtons.None;
        foreach (var (target, button) in ButtonMap)
            if (profile.Bindings.TryGetValue(target, out var b) && Read(input, b) > .5f) buttons |= button;
        // Opposing D-pad inputs cancel; never invent a direction.
        if ((buttons & (PadButtons.Up | PadButtons.Down)) == (PadButtons.Up | PadButtons.Down)) buttons &= ~(PadButtons.Up | PadButtons.Down);
        if ((buttons & (PadButtons.Left | PadButtons.Right)) == (PadButtons.Left | PadButtons.Right)) buttons &= ~(PadButtons.Left | PadButtons.Right);
        short Stick(Target t) => ToShort(Deadzone(Value(t), profile.Deadzone));
        float Value(Target t) => profile.Bindings.TryGetValue(t, out var b) ? Read(input, b, t is Target.LT or Target.RT) : 0;
        return new(buttons, Stick(Target.LX), Stick(Target.LY), Stick(Target.RX), Stick(Target.RY),
            (byte)Math.Round(Math.Clamp(Value(Target.LT), 0, 1) * 255), (byte)Math.Round(Math.Clamp(Value(Target.RT), 0, 1) * 255));
    }
    public static short ToShort(float v) => (short)Math.Round(Math.Clamp(v, -1, 1) * (v < 0 ? 32768 : 32767));
    public static float Deadzone(float v, float zone) => Math.Abs(v) <= zone ? 0 : Math.Sign(v) * (Math.Abs(v) - zone) / (1 - zone);
    public static float Normalize(int raw, InputBinding b, bool trigger)
    {
        var delta = raw - b.Center;
        var positive = b.PositiveEnd - b.Center;
        if (positive == 0) return 0;
        float v;
        if (trigger) v = Math.Clamp((float)delta / positive, 0, 1);
        else
        {
            var negative = b.NegativeEnd - b.Center;
            v = Math.Sign(delta) == Math.Sign(positive) ? (float)delta / positive : negative == 0 ? 0 : -(float)delta / negative;
            v = Math.Clamp(v, -1, 1);
        }
        return b.Invert ? trigger ? 1 - v : -v : v;
    }
    public static float Read(PhysicalState s, InputBinding b, bool trigger = false)
    {
        static bool Valid(int n, int length) => n >= 0 && n < length;
        return b.Kind switch
        {
            InputKind.Button when Valid(b.Index, s.Buttons.Length) => s.Buttons[b.Index] ? 1 : 0,
            InputKind.SdlButton when s.Device?.HasSdlMapping == true && Valid(b.Index, s.SdlButtons.Length) => s.SdlButtons[b.Index] ? 1 : 0,
            InputKind.Hat when Valid(b.Index, s.Hats.Length) => (s.Hats[b.Index] & b.HatMask) != 0 ? 1 : 0,
            InputKind.Axis when Valid(b.Index, s.Axes.Length) => Normalize(s.Axes[b.Index], b, trigger),
            InputKind.SdlAxis when s.Device?.HasSdlMapping == true && Valid(b.Index, s.SdlAxes.Length) => Normalize(s.SdlAxes[b.Index], b, trigger),
            _ => 0
        };
    }
    public static MappingProfile SdlDefault(DeviceInfo device)
    {
        var p = new MappingProfile { DeviceKey = device.Key, Name = "Layout SDL (verifique os comandos)" };
        // SDL_GamepadButton stable ABI: south/east/west/north, back, guide, start, sticks, shoulders, d-pad.
        (Target t, int i)[] buttons = [(Target.A,0),(Target.B,1),(Target.X,2),(Target.Y,3),(Target.Back,4),(Target.Start,6),
            (Target.L3,7),(Target.R3,8),(Target.LB,9),(Target.RB,10),(Target.Up,11),(Target.Down,12),(Target.Left,13),(Target.Right,14)];
        foreach (var (t,i) in buttons) p.Bindings[t] = new() { Kind = InputKind.SdlButton, Index = i };
        for (int i = 0; i < 6; i++) p.Bindings[Target.LX + i] = new() { Kind = InputKind.SdlAxis, Index = i, Invert = i is 1 or 3 };
        return p;
    }
}
