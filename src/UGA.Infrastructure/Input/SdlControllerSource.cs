using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using SDL3;
using UGA.Core;

namespace UGA.Infrastructure.Input;

public sealed class SdlControllerSource : IPhysicalControllerSource, IPhysicalControllerOutput
{
    private nint joystick, gamepad;
    private uint selectedId;
    private bool initialized;
    private long nextScan;
    private DeviceInfo[] devices = [];
    private DeviceInfo? selected;
    public IReadOnlyList<DeviceInfo> Devices => devices;
    public string PreferredDeviceKey { get; set; } = "";
    public ControllerOutputCapabilities OutputCapabilities
    {
        get
        {
            if (joystick == 0) return default;
            var props = SDL.GetJoystickProperties(joystick);
            return new(SDL.GetBooleanProperty(props, SDL.Props.JoystickCapRGBLedBoolean, false),
                SDL.GetBooleanProperty(props, SDL.Props.JoystickCapRumbleBoolean, false));
        }
    }
    public void SetLed(byte red, byte green, byte blue)
    {
        if (joystick == 0 || !SDL.SetJoystickLED(joystick, red, green, blue))
            throw new IOException("LED: " + SDL.GetError());
    }
    public void Rumble(ushort lowFrequency, ushort highFrequency, uint durationMs)
    {
        if (joystick == 0 || !SDL.RumbleJoystick(joystick, unchecked((short)lowFrequency), unchecked((short)highFrequency), checked((int)durationMs)))
            throw new IOException("Vibração: " + SDL.GetError());
    }
    public void Initialize()
    {
        if (initialized) return;
        SDL.SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
        // Avoid legacy XInput duplication of the generated Xbox, prefer HID/DirectInput for physical pads.
        SDL.SetHint("SDL_JOYSTICK_HIDAPI_PS4", "1");
        if (!SDL.Init(SDL.InitFlags.Gamepad)) throw new InvalidOperationException("SDL3: " + SDL.GetError());
        initialized = true;
        SDL.SetJoystickEventsEnabled(false); SDL.SetGamepadEventsEnabled(false);
    }
    private static string GuidText(SDL.GUID guid)
    { var bytes = new byte[33]; SDL.GUIDToString(guid, bytes, bytes.Length); return Encoding.ASCII.GetString(bytes).TrimEnd('\0'); }
    private void Scan()
    {
        var list = new List<DeviceInfo>();
        foreach (var id in SDL.GetJoysticks(out _) ?? [])
        {
            var vendor = SDL.GetJoystickVendorForID(id); var product = SDL.GetJoystickProductForID(id);
            var path = SDL.GetJoystickPathForID(id) ?? "";
            var identity = WindowsDeviceIdentity.Resolve(path);
            // SDL virtual flag only covers SDL virtual joysticks, so also check PnP ancestry and the output VID/PID.
            if (SDL.IsJoystickVirtual(id) || identity.IsVirtual || (vendor == 0x045E && product == 0x028E)) continue;
            var guid = GuidText(SDL.GetJoystickGUIDForID(id)); var name = SDL.GetJoystickNameForID(id) ?? "Controle sem nome";
            var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{guid}|{vendor}|{product}|{identity.InstancePath}|{path}")));
            list.Add(new(id, name, key, guid, vendor, product, path, identity.InstancePath, identity.IsBluetooth ? "Bluetooth" : "A verificar", SDL.IsGamepad(id)));
        }
        devices = list.ToArray();
    }
    public void Poll(PhysicalState state)
    {
        SDL.UpdateGamepads(); // With events disabled SDL explicitly updates joystick state without pumping video events.
        if (joystick != 0 && (!SDL.JoystickConnected(joystick) ||
            (!string.IsNullOrEmpty(PreferredDeviceKey) && selected?.Key != PreferredDeviceKey))) Close();
        if (Stopwatch.GetTimestamp() >= nextScan)
        {
            Scan(); nextScan = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
        }
        if (joystick == 0)
        {
            var candidate = string.IsNullOrEmpty(PreferredDeviceKey) ? devices.FirstOrDefault() : devices.FirstOrDefault(x => x.Key == PreferredDeviceKey);
            if (candidate is null) { state.Clear(); return; }
            joystick = SDL.OpenJoystick(candidate.InstanceId);
            if (joystick == 0) throw new InvalidOperationException("Não foi possível abrir controle: " + SDL.GetError());
            selectedId = candidate.InstanceId;
            gamepad = candidate.HasSdlMapping ? SDL.OpenGamepad(selectedId) : 0;
            var connection = SDL.GetJoystickConnectionState(joystick);
            var transport = candidate.Connection == "Bluetooth" || candidate.Path.Contains("BTH", StringComparison.OrdinalIgnoreCase) ? "Bluetooth" : connection switch
            { SDL.JoystickConnectionState.Wireless => "Sem fio (Bluetooth ou receptor)", SDL.JoystickConnectionState.Wired => "USB / com fio", _ => "Transporte não informado pelo dispositivo" };
            selected = candidate with { Connection = transport, HasSdlMapping = gamepad != 0 };
            state.Axes = new short[Math.Clamp(SDL.GetNumJoystickAxes(joystick), 0, 256)];
            state.Buttons = new bool[Math.Clamp(SDL.GetNumJoystickButtons(joystick), 0, 256)];
            state.Hats = new byte[Math.Clamp(SDL.GetNumJoystickHats(joystick), 0, 256)];
        }
        state.Device = selected;
        for (int i = 0; i < state.Axes.Length; i++) state.Axes[i] = SDL.GetJoystickAxis(joystick, i);
        for (int i = 0; i < state.Buttons.Length; i++) state.Buttons[i] = SDL.GetJoystickButton(joystick, i);
        for (int i = 0; i < state.Hats.Length; i++) state.Hats[i] = (byte)SDL.GetJoystickHat(joystick, i);
        if (gamepad != 0)
        {
            for (int i = 0; i < 6; i++) state.SdlAxes[i] = SDL.GetGamepadAxis(gamepad, (SDL.GamepadAxis)i);
            for (int i = 0; i < state.SdlButtons.Length; i++) state.SdlButtons[i] = SDL.GetGamepadButton(gamepad, (SDL.GamepadButton)i);
        }
        else { Array.Clear(state.SdlAxes); Array.Clear(state.SdlButtons); }
    }
    private void Close()
    {
        if (joystick != 0 && OutputCapabilities.Rumble) SDL.RumbleJoystick(joystick, 0, 0, 0);
        if (gamepad != 0) SDL.CloseGamepad(gamepad);
        if (joystick != 0) SDL.CloseJoystick(joystick);
        gamepad = joystick = 0; selected = null; selectedId = 0;
    }
    public void Dispose() { Close(); if (initialized) SDL.Quit(); initialized = false; }
}
