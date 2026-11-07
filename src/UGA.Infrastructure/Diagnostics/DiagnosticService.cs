using System.Diagnostics;
using System.Text.Json;
using UGA.Core;

namespace UGA.Infrastructure.Diagnostics;

public static class DiagnosticService
{
    public const string ViGEmDownload = "https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0";
    public static string[] FindConflicts()
    {
        string[] names = ["DS4Windows", "DSX", "InputMapper", "reWASD", "x360ce", "JoyToKey", "steam"];
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            using (process)
                try { var name = process.ProcessName; if (names.Any(x => name.Equals(x, StringComparison.OrdinalIgnoreCase))) found.Add(name); }
                catch (InvalidOperationException) { }
        }
        return found.ToArray();
    }
    public static string Export(EngineSnapshot snapshot, string driver, string[] conflicts)
    {
        var d = snapshot.Input.Device;
        // Deliberately exclude PnP paths, serial numbers, user/profile paths and free-form exception text.
        var report = new
        {
            Application = "Universal Gamepad Adapter 1.0.0", TimeUtc = DateTime.UtcNow, OS = Environment.OSVersion.VersionString,
            Runtime = Environment.Version.ToString(), Driver = driver.Split(':')[0], VirtualActive = snapshot.Running,
            snapshot.ActualHz, HasError = snapshot.Error.Length > 0, Conflicts = conflicts,
            Device = d is null ? null : new { d.Name, d.Guid, d.Vendor, d.Product, d.Connection, d.HasSdlMapping },
            snapshot.Input.Axes, snapshot.Input.Buttons, snapshot.Input.Hats, snapshot.Input.SdlAxes,
            snapshot.Output, Mappings = snapshot.Profile?.Bindings, Deadzone = snapshot.Profile?.Deadzone
        };
        return JsonSerializer.Serialize(report, JsonFormat.Options);
    }
}
