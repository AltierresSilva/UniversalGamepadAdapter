using UGA.Core;
using UGA.Core.Storage;
using UGA.Infrastructure.Diagnostics;
using Xunit;

namespace UGA.Tests;

public class StorageTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "UGA-Tests-" + Guid.NewGuid().ToString("N"));
    [Fact] public void ProfileAndCalibrationRoundTrip()
    {
        var store = new JsonStore(directory); var p = MappingService.SdlDefault(MappingTests.Device) with { Deadzone = .17f };
        p.Bindings[Target.RX] = new() { Kind = InputKind.Axis, Index = 4, Center = 234, NegativeEnd = -30000, PositiveEnd = 31000, Invert = true };
        store.Save(p); var loaded = store.Load(p.DeviceKey)!;
        Assert.Equal(p.Deadzone, loaded.Deadzone); Assert.Equal(p.Bindings[Target.RX], loaded.Bindings[Target.RX]); Assert.True(loaded.Complete);
        Assert.Empty(Directory.GetFiles(directory, "*.tmp", SearchOption.AllDirectories));
    }
    [Fact] public void InvalidJsonIsPreservedAndDefaultsRecovered()
    {
        var store = new JsonStore(directory); File.WriteAllText(Path.Combine(directory,"settings.json"), "{invalid");
        Assert.Equal(125, store.LoadSettings().PollHz); Assert.Single(Directory.GetFiles(directory,"*.invalid-*")); Assert.NotEmpty(store.LastRecovery);
    }
    [Fact] public void InvalidProfileIsQuarantined()
    {
        var store = new JsonStore(directory); store.Save(MappingService.SdlDefault(MappingTests.Device));
        var file = Directory.GetFiles(Path.Combine(directory,"Profiles"))[0]; File.WriteAllText(file, "null");
        Assert.Null(store.Load(MappingTests.Device.Key)); Assert.Single(Directory.GetFiles(Path.Combine(directory,"Profiles"),"*.invalid-*"));
    }
    [Fact] public void AtomicReplacementAndSafeFilenames()
    {
        var store = new JsonStore(directory); store.SaveSettings(new() { AutoStart = true }); store.SaveSettings(new() { PollHz = 250, Theme = "Light" });
        Assert.Equal(250, store.LoadSettings().PollHz); Assert.False(store.LoadSettings().AutoStart); Assert.Equal("Light", store.LoadSettings().Theme);
        store.Save(new() { DeviceKey = "../../bad:key" }); Assert.Single(Directory.GetFiles(Path.Combine(directory,"Profiles"),"*.json"));
    }
    [Fact] public void DiagnosticExportExcludesPathsSerialAndErrorText()
    {
        var input = MappingTests.Input(); input.Device = input.Device! with { Path = @"C:\Users\private\serial-secret", InstancePath = "SERIAL-SECRET" };
        var json = DiagnosticService.Export(new(input, default, null, false, "ok", @"C:\Users\private error", [], 125), "driver", []);
        Assert.DoesNotContain("serial-secret", json, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("private", json);
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
