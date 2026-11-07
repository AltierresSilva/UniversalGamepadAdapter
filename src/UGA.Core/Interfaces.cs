namespace UGA.Core;

public interface IPhysicalControllerSource : IDisposable
{
    void Initialize();
    IReadOnlyList<DeviceInfo> Devices { get; }
    string PreferredDeviceKey { get; set; }
    void Poll(PhysicalState destination);
}
public interface IVirtualControllerBackend : IDisposable
{
    bool Connected { get; }
    string Probe();
    void Connect();
    void Submit(in VirtualState state);
    void Disconnect();
}
public interface IMappingService { VirtualState Map(PhysicalState input, MappingProfile profile); }
public readonly record struct ControllerOutputCapabilities(bool RgbLed, bool Rumble);
public interface IPhysicalControllerOutput
{
    ControllerOutputCapabilities OutputCapabilities { get; }
    void SetLed(byte red, byte green, byte blue);
    void Rumble(ushort lowFrequency, ushort highFrequency, uint durationMs);
}
public interface IVirtualControllerFeedback
{
    event Action<byte, byte>? FeedbackReceived;
}
public interface IProfileStore
{
    MappingProfile? Load(string deviceKey);
    void Save(MappingProfile profile);
}
