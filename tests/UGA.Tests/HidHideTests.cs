using System.Text.Json;
using UGA.Core;
using UGA.Infrastructure.HidHide;
using UGA.Infrastructure.Input;
using Xunit;

namespace UGA.Tests;

public sealed class HidHideTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "UGA-HidHide-Tests-" + Guid.NewGuid().ToString("N"));
    private readonly string app;
    private const string PhysicalPath = @"HID\VID_054C&PID_09CC\TEST";
    private const string OtherDevice = @"HID\OTHER\DEVICE";
    private readonly HashSet<string> apps = new(StringComparer.OrdinalIgnoreCase), devices = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> calls = [];
    private bool cloak, inverse, failHide;
    public HidHideTests() { Directory.CreateDirectory(root); app = Path.Combine(root,"UGA-test.exe"); File.WriteAllBytes(app, []); }
    private DeviceInfo Device => MappingTests.Device with { Path = "test-interface", InstancePath = PhysicalPath, Vendor = 0x054C, Product = 0x09CC };
    private HidHideService Service(bool virtualDevice = false) => new(root, Run, _ => new(PhysicalPath, virtualDevice, true, true));
    private Task<string> Run(string[] args)
    {
        var lines = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string command = args[i]; calls.Add(command);
            switch (command)
            {
                case "--dev-gaming": lines.Add(JsonSerializer.Serialize(new[] { new { devices = new[] { new { deviceInstancePath = PhysicalPath } } } })); break;
                case "--app-list": lines.AddRange(apps.Select(x => $"--app-reg \"{x}\"")); break;
                case "--dev-list": lines.AddRange(devices.Select(x => $"--dev-hide \"{x}\"")); break;
                case "--cloak-state": lines.Add(cloak ? "--cloak-on" : "--cloak-off"); break;
                case "--inv-state": lines.Add(inverse ? "--inv-on" : "--inv-off"); break;
                case "--app-reg": apps.Add(args[++i]); break;
                case "--app-unreg": apps.Remove(args[++i]); break;
                case "--dev-hide": devices.Add(args[++i]); if (failHide) throw new IOException("simulated partial write"); break;
                case "--dev-unhide": devices.Remove(args[++i]); break;
                case "--cloak-on": cloak = true; break;
                case "--cloak-off": cloak = false; break;
                default: throw new InvalidOperationException("Unsupported test command: " + command);
            }
        }
        return Task.FromResult(string.Join(Environment.NewLine,lines));
    }
    [Fact] public async Task WhitelistsFirstPreservesThirdPartyEntriesAndUndoesOnlyOwnChanges()
    {
        apps.Add(@"C:\Other\Tool.exe"); devices.Add(OtherDevice); cloak = true;
        var service = Service(); await service.Apply(Device,app);
        Assert.True(service.HasChanges); Assert.Contains(app,apps); Assert.Contains(PhysicalPath,devices); Assert.Contains(OtherDevice,devices);
        Assert.True(calls.IndexOf("--app-reg") < calls.IndexOf("--dev-hide"));
        await service.Undo(); Assert.False(service.HasChanges); Assert.DoesNotContain(app,apps); Assert.DoesNotContain(PhysicalPath,devices);
        Assert.Contains(@"C:\Other\Tool.exe",apps); Assert.Contains(OtherDevice,devices); Assert.True(cloak);
    }
    [Fact] public async Task InitialEmptyConfigurationReturnsToOriginalState()
    {
        var service = Service(); await service.Apply(Device,app); Assert.True(cloak); await service.Undo();
        Assert.False(cloak); Assert.Empty(apps); Assert.Empty(devices);
    }
    [Fact] public async Task NeverUnregistersPreexistingApplicationOrDevice()
    {
        apps.Add(app); devices.Add(PhysicalPath); cloak = true; var service = Service();
        await service.Apply(Device,app); await service.Undo(); Assert.Contains(app,apps); Assert.Contains(PhysicalPath,devices); Assert.True(cloak);
    }
    [Fact] public async Task RefusesVirtualDeviceBeforeAnyCommand()
    { await Assert.ThrowsAsync<InvalidOperationException>(() => Service(true).Apply(Device,app)); Assert.Empty(calls); }
    [Fact] public async Task RefusesInverseModeOrGlobalActivationAffectingOthers()
    {
        inverse = true; await Assert.ThrowsAsync<InvalidOperationException>(() => Service().Apply(Device,app)); Assert.Empty(apps);
        inverse = false; devices.Add(OtherDevice); await Assert.ThrowsAsync<InvalidOperationException>(() => Service().Apply(Device,app)); Assert.Empty(apps); Assert.False(cloak);
    }
    [Fact] public async Task PartialFailureKeepsJournalAndIsRecoverable()
    {
        var service = Service(); failHide = true; await Assert.ThrowsAsync<IOException>(() => service.Apply(Device,app));
        Assert.True(service.HasChanges); failHide = false; await service.Undo(); Assert.Empty(apps); Assert.Empty(devices); Assert.False(service.HasChanges);
    }
    [Fact] public async Task UndoDoesNotDisableCloakingForNewThirdPartyDevice()
    {
        var service = Service(); await service.Apply(Device,app); devices.Add(OtherDevice); await service.Undo();
        Assert.True(cloak); Assert.Contains(OtherDevice,devices); Assert.DoesNotContain(PhysicalPath,devices);
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root,true); }
}
