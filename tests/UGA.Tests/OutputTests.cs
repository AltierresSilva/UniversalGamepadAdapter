using UGA.Core;
using UGA.Core.Storage;
using Xunit;

namespace UGA.Tests;

public class OutputTests
{
    private sealed class Source : IPhysicalControllerSource, IPhysicalControllerOutput
    {
        public DeviceInfo? Device = MappingTests.Device;
        public bool FailLed, FailRumble;
        public ControllerOutputCapabilities OutputCapabilities { get; set; } = new(true, true);
        public List<(byte, byte, byte)> Colors = [];
        public List<(ushort Low, ushort High, uint Duration)> Pulses = [];
        public IReadOnlyList<DeviceInfo> Devices => Device is { } d ? [d] : [];
        public string PreferredDeviceKey { get; set; } = "";
        public void Initialize() { }
        public void Poll(PhysicalState destination) => destination.Device = Device;
        public void SetLed(byte red, byte green, byte blue)
        { if (FailLed) throw new IOException("LED refused"); Colors.Add((red, green, blue)); }
        public void Rumble(ushort low, ushort high, uint duration)
        { if (FailRumble) throw new IOException("rumble refused"); Pulses.Add((low, high, duration)); }
        public void Dispose() { }
    }
    private sealed class Backend : IVirtualControllerBackend, IVirtualControllerFeedback
    {
        public bool Connected { get; private set; }
        public event Action<byte, byte>? FeedbackReceived;
        public void Emit(byte low, byte high) => FeedbackReceived?.Invoke(low, high);
        public string Probe() => "fake";
        public void Connect() => Connected = true;
        public void Submit(in VirtualState state) { }
        public void Disconnect() => Connected = false;
        public void Dispose() => Disconnect();
    }
    private sealed class Store : IProfileStore
    {
        public MappingProfile? Load(string key) => null;
        public void Save(MappingProfile profile) { }
    }
    [Fact] public void LedIsOptInAndReappliedOnReconnectEvenWithSameDeviceKey()
    {
        var source = new Source(); using var engine = new ControllerEngine(source, new Backend(), new Store(), new());
        engine.Step(); Assert.Empty(source.Colors);
        engine.Configure(new() { CustomLedEnabled = true, LedColor = 0x12AB34 }); engine.Step();
        Assert.Equal(((byte)0x12, (byte)0xAB, (byte)0x34), Assert.Single(source.Colors));
        source.Device = MappingTests.Device with { InstanceId = MappingTests.Device.InstanceId + 1 }; engine.Step();
        Assert.Equal(2, source.Colors.Count);
    }
    [Fact] public void UnsupportedOutputsAreNeverCalled()
    {
        var source = new Source { OutputCapabilities = default }; using var engine = new ControllerEngine(source, new Backend(), new Store(), new() { CustomLedEnabled = true });
        engine.Step(); engine.TestVibration(MappingTests.Device.Key); engine.Step();
        Assert.Empty(source.Colors); Assert.Empty(source.Pulses);
    }
    [Fact] public void GameFeedbackScalesBothMotorsAndZeroStopsThem()
    {
        var source = new Source(); var backend = new Backend(); using var engine = new ControllerEngine(source, backend, new Store(), new() { AutoStart = true, VibrationPercent = 50 });
        engine.Step(); backend.Emit(255, 128); engine.Step();
        Assert.Equal(((ushort)32767, (ushort)16448, 250u), source.Pulses[^1]);
        backend.Emit(0, 0); engine.Step(); Assert.Equal((0, 0, 250u), source.Pulses[^1]);
        engine.Configure(new() { VibrationPercent = 0 }); engine.Step(); backend.Emit(255, 255); engine.Step();
        Assert.Equal((0, 0, 250u), source.Pulses[^1]);
    }
    [Fact] public void ManualTestIsBoundedAndStopAndDisposeNeutralizeIt()
    {
        var source = new Source(); var engine = new ControllerEngine(source, new Backend(), new Store(), new() { VibrationPercent = 25 });
        engine.Step(); engine.TestVibration(MappingTests.Device.Key); engine.Step();
        Assert.Equal(((ushort)16383, (ushort)16383, 500u), source.Pulses[^1]);
        engine.StopVibration(); engine.Step(); Assert.Equal((0, 0, 0u), source.Pulses[^1]);
        engine.TestVibration(MappingTests.Device.Key); engine.Step(); engine.Dispose(); Assert.Equal((0, 0, 0u), source.Pulses[^1]);
    }
    [Fact] public void StoppingEmulationRejectsLateFeedbackAndStopsMotors()
    {
        var source = new Source(); var backend = new Backend(); using var engine = new ControllerEngine(source, backend, new Store(), new() { AutoStart = true });
        engine.Step(); backend.Emit(255, 255); engine.Step(); engine.RequestEmulation(false); engine.Step();
        Assert.Equal((0, 0, 0u), source.Pulses[^1]); int count = source.Pulses.Count;
        backend.Emit(255, 255); engine.Step(); Assert.Equal(count, source.Pulses.Count);
    }
    [Fact] public void RequestsForDisconnectedControllerAreDiscarded()
    {
        var source = new Source(); using var engine = new ControllerEngine(source, new Backend(), new Store(), new());
        engine.Step(); engine.TestVibration("other-device"); engine.Step(); Assert.Empty(source.Pulses);
        source.Device = null; engine.TestVibration(MappingTests.Device.Key); engine.Step(); Assert.Empty(source.Pulses);
    }
    [Fact] public void OutputFailuresDoNotStopInputOrRetryEveryPoll()
    {
        var source = new Source { FailLed = true, FailRumble = true }; var backend = new Backend();
        using var engine = new ControllerEngine(source, backend, new Store(), new() { AutoStart = true, CustomLedEnabled = true });
        engine.Step(); engine.Publish(); Assert.True(engine.Snapshot.Running); Assert.Empty(engine.Snapshot.Error);
        Assert.Contains("rumble refused", engine.Snapshot.OutputStatus);
        source.FailRumble = false; backend.Emit(255, 255); engine.Step(); Assert.Empty(source.Pulses);
        engine.TestVibration(MappingTests.Device.Key); engine.Step(); Assert.NotEmpty(source.Pulses);
    }
    [Fact] public void OutputSettingsPersistAndValidate()
    {
        string path = Path.Combine(Path.GetTempPath(), "UGA-Outputs-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonStore(path); var settings = new AppSettings { CustomLedEnabled = true, LedColor = 0xCC3377, VibrationPercent = 63 };
            store.SaveSettings(settings); Assert.Equal(settings, store.LoadSettings());
            Assert.Throws<InvalidDataException>(() => (settings with { VibrationPercent = 101 }).Validate());
            Assert.Throws<InvalidDataException>(() => (settings with { LedColor = -1 }).Validate());
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
}
