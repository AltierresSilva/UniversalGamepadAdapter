using UGA.Core;
using Xunit;

namespace UGA.Tests;

public class EngineTests
{
    private sealed class Source : IPhysicalControllerSource
    {
        public bool Present = true, Fail; public int Initializes, Disposals;
        public PhysicalState State = MappingTests.Input();
        public IReadOnlyList<DeviceInfo> Devices => Present ? [MappingTests.Device] : [];
        public string PreferredDeviceKey { get; set; } = "";
        public void Initialize() => Initializes++;
        public void Poll(PhysicalState destination)
        { if (Fail) throw new IOException("Bluetooth simulated failure"); destination.Device = Present ? MappingTests.Device : null; destination.SdlButtons = State.SdlButtons; destination.SdlAxes = State.SdlAxes; }
        public void Dispose() => Disposals++;
    }
    private sealed class Backend : IVirtualControllerBackend
    {
        public bool Connected { get; private set; }
        public bool FailConnect, FailSubmit; public int Connects, Disconnects;
        public List<VirtualState> Reports = [];
        public string Probe() => "fake";
        public void Connect() { Connects++; if (FailConnect) throw new IOException("no driver"); Connected = true; }
        public void Submit(in VirtualState state) { if (FailSubmit) throw new IOException("backend failure"); Reports.Add(state); }
        public void Disconnect() { Connected = false; Disconnects++; }
        public void Dispose() => Disconnect();
    }
    private sealed class Store : IProfileStore
    { public MappingProfile? Profile; public MappingProfile? Load(string key) => Profile; public void Save(MappingProfile profile) => Profile = profile; }
    [Fact] public void StartsNeutralCreatesOnlyOneVirtualAndStopsNeutral()
    {
        var source = new Source(); source.State.SdlButtons[0] = true; var backend = new Backend();
        using var engine = new ControllerEngine(source, backend, new Store(), new());
        engine.RequestEmulation(true); engine.Step(); engine.Step(); engine.Publish();
        Assert.Equal(1, backend.Connects); Assert.Equal(default, backend.Reports[0]); Assert.Equal(PadButtons.A, backend.Reports[^1].Buttons); Assert.True(engine.Snapshot.Running);
        engine.RequestEmulation(false); engine.Step(); engine.Publish(); Assert.Equal(default, backend.Reports[^1]); Assert.False(engine.Snapshot.Running); Assert.Equal(1, source.Initializes);
    }
    [Fact] public void DisconnectNeutralizesAndReconnectsWithoutNewReader()
    {
        var source = new Source(); var backend = new Backend(); using var engine = new ControllerEngine(source, backend, new Store(), new() { AutoStart = true });
        engine.Step(); source.State.SdlButtons[0] = true; engine.Step(); source.Present = false; engine.Step();
        Assert.Equal(default, backend.Reports[^1]); Assert.False(backend.Connected);
        source.Present = true; engine.Step(); Assert.True(backend.Connected); Assert.Equal(2, backend.Connects); Assert.Equal(1, source.Initializes);
    }
    [Fact] public void MissingDriverDoesNotCrashOrRetryContinuously()
    {
        var backend = new Backend { FailConnect = true }; using var engine = new ControllerEngine(new Source(), backend, new Store(), new() { AutoStart = true });
        engine.Step(); engine.Step(); engine.Publish(); Assert.False(engine.Snapshot.Running); Assert.Contains("no driver", engine.Snapshot.Error); Assert.Equal(1, backend.Connects);
        backend.FailConnect = false; engine.RequestEmulation(true); engine.Step(); Assert.True(backend.Connected);
    }
    [Fact] public void SubmitFailureDisconnectsAndRequiresExplicitRestart()
    {
        var backend = new Backend(); using var engine = new ControllerEngine(new Source(), backend, new Store(), new() { AutoStart = true });
        engine.Step(); backend.FailSubmit = true; engine.Step(); Assert.False(backend.Connected); backend.FailSubmit = false; engine.Step(); Assert.False(backend.Connected);
        engine.RequestEmulation(true); engine.Step(); Assert.True(backend.Connected);
    }
    [Fact] public void SourceFailureAlwaysRemovesVirtualController()
    {
        var source = new Source(); var backend = new Backend(); using var engine = new ControllerEngine(source, backend, new Store(), new() { AutoStart = true });
        engine.Step(); source.Fail = true; engine.Step(); Assert.False(backend.Connected); Assert.Equal(default, backend.Reports[^1]);
    }
    [Fact] public void MappingSuspensionAndIncompleteProfilesBlockOutput()
    {
        var backend = new Backend(); using var engine = new ControllerEngine(new Source(), backend, new Store(), new() { AutoStart = true });
        engine.Step(); engine.Suspend(true); engine.Step(); Assert.False(backend.Connected);
        engine.Suspend(false); engine.ApplyProfile(new() { DeviceKey = MappingTests.Device.Key }); engine.Step(); Assert.False(backend.Connected);
    }
    [Fact] public void DisposalReleasesBackendAndSource()
    {
        var source = new Source(); var backend = new Backend(); var engine = new ControllerEngine(source, backend, new Store(), new() { AutoStart = true }); engine.Step(); engine.Dispose();
        Assert.Equal(default, backend.Reports[^1]); Assert.False(backend.Connected); Assert.Equal(1, source.Disposals);
    }
}
